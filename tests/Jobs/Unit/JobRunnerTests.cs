using Aviant.Application.Jobs;
using Aviant.Infrastructure.Jobs;
using AwesomeAssertions;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Xunit;

namespace Aviant.Tests.Jobs.Unit;

public sealed class JobRunnerTests
{
    [Fact]
    public void AnEnqueuedJobReceivesTheServersCancellationToken()
    {
        var client = new RecordingClient();
        var runner = new JobRunner(client, new RecordingRecurringManager());

        runner.Run<SendReceipt, SendReceiptOptions>(options => options.OrderId = 42);

        var job = client.Jobs.Should().ContainSingle().Subject;
        job.Method.GetParameters().Select(p => p.ParameterType)
           .Should().Equal(typeof(SendReceiptOptions), typeof(CancellationToken));
        job.Args[0].Should().BeOfType<SendReceiptOptions>().Which.OrderId.Should().Be(42);
    }

    [Fact]
    public void ADelayedJobIsScheduledRelativeToNow()
    {
        var client = new RecordingClient();
        var runner = new JobRunner(client, new RecordingRecurringManager());

        runner.RunWithDelay<SendReceipt, SendReceiptOptions>(TimeSpan.FromMinutes(5));

        client.States.Should().ContainSingle().Which.Should().BeOfType<ScheduledState>()
           .Which.EnqueueAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(5), TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void ARecurringJobRunsInTheGivenTimeZoneAndQueue()
    {
        var manager = new RecordingRecurringManager();
        var runner  = new JobRunner(new RecordingClient(), manager);
        var athens  = TimeZoneInfo.CreateCustomTimeZone("athens", TimeSpan.FromHours(2), "athens", "athens");

        runner.RunRecurring<NightlySweep>("nightly-sweep", Cron.Daily(3), athens, queue: "maintenance");

        var (id, job, cron, options) = manager.Recurring.Should().ContainSingle().Subject;
        id.Should().Be("nightly-sweep");
        cron.Should().Be(Cron.Daily(3));
        job.Type.Should().Be<NightlySweep>();
        job.Queue.Should().Be("maintenance");
        options.TimeZone.Should().Be(athens);
    }

    public sealed class SendReceiptOptions : IJobOptions
    {
        public int OrderId { get; set; }
    }

    public sealed class SendReceipt : IJob<SendReceiptOptions>
    {
        public Task PerformAsync(SendReceiptOptions jobOptions, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    public sealed class NightlySweep : IRecurringJob
    {
        public Task RunAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class RecordingClient : IBackgroundJobClient
    {
        public List<Job> Jobs { get; } = [];

        public List<IState> States { get; } = [];

        public string Create(Job job, IState state)
        {
            Jobs.Add(job);
            States.Add(state);

            return Jobs.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        public bool ChangeState(string jobId, IState state, string expectedState) => true;
    }

    private sealed class RecordingRecurringManager : IRecurringJobManager
    {
        public List<(string Id, Job Job, string Cron, RecurringJobOptions Options)> Recurring { get; } = [];

        public void AddOrUpdate(string recurringJobId, Job job, string cronExpression, RecurringJobOptions options) =>
            Recurring.Add((recurringJobId, job, cronExpression, options));

        public void Trigger(string recurringJobId)
        { }

        public void RemoveIfExists(string recurringJobId)
        { }
    }
}
