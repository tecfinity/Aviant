using Aviant.Application.MultiTenancy;
using Aviant.Core.MultiTenancy;
using Aviant.Infrastructure.MultiTenancy;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aviant.Tests.MultiTenancy.Unit;

public sealed class TenancyTests
{
    private static readonly Guid Acme   = Guid.NewGuid();
    private static readonly Guid Globex = Guid.NewGuid();

    [Fact]
    public async Task EachContextSeesOnlyItsOwnTenantsRows()
    {
        var database = await SeedAsync();

        // The model, and its filters, is cached after the first context: the second must still see its own tenant.
        await using var acme   = Invoices.For(database, new Scope(Acme));
        await using var globex = Invoices.For(database, new Scope(Globex));

        (await acme.Invoices.Select(i => i.Number).ToListAsync(TestContext.Current.CancellationToken)).Should().Equal("A-1");
        (await globex.Invoices.Select(i => i.Number).ToListAsync(TestContext.Current.CancellationToken)).Should().Equal("G-1");
    }

    [Fact]
    public async Task AnOperatorWithNoTenantChosenSeesEveryTenant()
    {
        var database = await SeedAsync();
        await using var operatorContext = Invoices.For(database, new Scope(null, CanCross: true));

        (await operatorContext.Invoices.CountAsync(TestContext.Current.CancellationToken)).Should().Be(3);
    }

    [Fact]
    public async Task RowsWithoutATenantAreHiddenUnlessAsked()
    {
        var database = await SeedAsync();
        await using var strict  = Invoices.For(database, new Scope(Acme));
        await using var lenient = Invoices.For(database, new Scope(Acme), includeRowsWithoutTenant: true);

        (await strict.Invoices.CountAsync(TestContext.Current.CancellationToken)).Should().Be(1);
        (await lenient.Invoices.CountAsync(TestContext.Current.CancellationToken)).Should().Be(2);
    }

    [Fact]
    public async Task ANewRowIsStampedWithTheCurrentTenant()
    {
        var database = Guid.NewGuid().ToString("N");
        await using var context = Invoices.For(database, new Scope(Acme));
        var invoice = new Invoice { Id = 1, Number = "A-2" };

        context.Invoices.Add(invoice);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        invoice.TenantId.Should().Be(Acme);
    }

    [Fact]
    public async Task ARowCannotBeMovedToAnotherTenant()
    {
        var database = await SeedAsync();
        await using var context = Invoices.For(database, new Scope(Acme));
        var invoice = await context.Invoices.SingleAsync(TestContext.Current.CancellationToken);

        invoice.TenantId = Globex;
        var act = () => context.SaveChangesAsync(TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Message.Should().Contain("tenant ownership is immutable");
    }

    [Fact]
    public async Task AJobEntersItsTenantEvenForAContextBuiltBeforeIt()
    {
        var database = await SeedAsync();
        var services = new ServiceCollection();
        services.AddAviantMultiTenancy<NoRequest>();
        services.AddScoped(provider => Invoices.For(database, provider.GetRequiredService<ITenantScope>()));
        services.AddScoped<CountInvoices>();
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        // Hangfire builds the job, and with it the context, before PerformAsync enters the tenant.
        var job = scope.ServiceProvider.GetRequiredService<CountInvoices>();
        await job.PerformAsync(new CountOptions { TenantId = Globex }, TestContext.Current.CancellationToken);

        job.Seen.Should().Equal("G-1");
    }

    [Fact]
    public void AScopeCannotSwitchTenants()
    {
        var scope = new BackgroundTenantScope();
        scope.Enter(Acme);

        var act = () => scope.Enter(Globex);

        act.Should().Throw<InvalidOperationException>();
    }

    private static async Task<string> SeedAsync()
    {
        var database = Guid.NewGuid().ToString("N");
        await using var context = Invoices.For(database, new Scope(null, CanCross: true));
        context.Invoices.AddRange(
            new Invoice { Id = 1, Number = "A-1", TenantId = Acme },
            new Invoice { Id = 2, Number = "G-1", TenantId = Globex },
            new Invoice { Id = 3, Number = "legacy" });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        return database;
    }

    private sealed record Scope(Guid? TenantId, bool CanCross = false) : ITenantScope
    {
        public bool CanCrossTenantBoundary => CanCross;
    }

    private sealed class NoRequest : ITenantScope
    {
        public Guid? TenantId => null;

        public bool CanCrossTenantBoundary => false;
    }

    public sealed class Invoice : ITenantOwned
    {
        public int Id { get; set; }

        public string Number { get; set; } = string.Empty;

        public Guid? TenantId { get; set; }
    }

    public sealed class CountOptions : ITenantScopedJobOptions
    {
        public Guid TenantId { get; set; }
    }

    private sealed class CountInvoices(IServiceProvider services, Ledger context) : TenantScopedJob<CountOptions>(services)
    {
        public List<string> Seen { get; } = [];

        protected override async Task RunAsync(CountOptions jobOptions, IServiceProvider scope, CancellationToken cancellationToken) =>
            Seen.AddRange(await context.Invoices.Select(i => i.Number).ToListAsync(cancellationToken));
    }

    public class Ledger(DbContextOptions options, ITenantScope scope) : DbContext(options)
    {
        private readonly TenantFilter _tenant = new(scope);

        public DbSet<Invoice> Invoices => Set<Invoice>();

        protected virtual bool IncludeRowsWithoutTenant => false;

        private TenantFilter Tenant => _tenant;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Invoice>().Property(i => i.Id).ValueGeneratedNever();
            modelBuilder.UseTenantFilter(this, () => Tenant, IncludeRowsWithoutTenant);
        }
    }

    // EF Core caches one model per context type, so a different filter needs a different type.
    public sealed class LenientLedger(DbContextOptions options, ITenantScope scope) : Ledger(options, scope)
    {
        protected override bool IncludeRowsWithoutTenant => true;
    }

    private static class Invoices
    {
        public static Ledger For(string database, ITenantScope scope, bool includeRowsWithoutTenant = false)
        {
            var options = new DbContextOptionsBuilder()
               .UseInMemoryDatabase(database)
               .AddInterceptors(new TenantStampingInterceptor(scope))
               .Options;

            return includeRowsWithoutTenant ? new LenientLedger(options, scope) : new Ledger(options, scope);
        }
    }
}
