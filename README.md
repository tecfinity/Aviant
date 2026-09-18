# Aviant Library

A collection of .NET libraries for building clean, scalable applications using DDD, CQRS, and Event Sourcing.

> **Source-only:** Aviant is currently consumed by adding it as a git submodule. NuGet packages are planned.
> See [CleanDDDArchitecture](https://github.com/panosru/CleanDDDArchitecture) for full usage examples.

## Module Overview

```
src/
├── Kernel/          # Core abstractions, behaviours, pipeline
│   ├── Core/        # Entities, Value Objects, Specifications, Timing
│   ├── Application/ # MediatR pipeline behaviours, Commands, Queries
│   └── Infrastructure/ # Cross-cutting DI helpers
├── DDD/             # Domain-Driven Design building blocks
├── EventSourcing/   # Event-sourced aggregates and persistence
├── Identity/        # User identity and JWT authentication
├── Persistence/     # CQRS repositories and Unit of Work
├── Email/           # SMTP email service
└── Jobs/            # Hangfire background job runner
```

## Quick Usage Examples

### Add as a Git Submodule

```bash
git submodule add https://github.com/panosru/Aviant.git Library/Aviant
git submodule update --init --recursive
```

### Register a Domain with the Kernel

```csharp
// In your CrossCutting project
public static IServiceCollection AddMyDomain(this IServiceCollection services)
{
    services.AddDbContext<MyDbContext>(...);
    services.AddScoped<IMyRepository, MyRepository>();
    return services;
}
```

### CQRS — Command + Handler

```csharp
// Command
public sealed record CreateWeatherCommand(string City, double Temperature)
    : IRequest<WeatherDto>;

// Handler
public sealed class CreateWeatherCommandHandler
    : IRequestHandler<CreateWeatherCommand, WeatherDto>
{
    public async Task<WeatherDto> Handle(
        CreateWeatherCommand request,
        CancellationToken cancellationToken)
    {
        // ... domain logic
    }
}
```

### Event Sourcing — Aggregate

```csharp
public sealed class AccountAggregate : AggregateRoot<AccountAggregate, AccountId>
{
    public string Email { get; private set; } = string.Empty;

    public void Register(string email)
    {
        Apply(new AccountRegisteredEvent(Id, email));
    }

    protected override void When(IDomainEvent @event)
    {
        if (@event is AccountRegisteredEvent e)
            Email = e.Email;
    }
}
```

### MediatR Pipeline

Register the whole pipeline with one call, passing every assembly that holds requests and handlers:

```csharp
services.AddAviantCqrs(
    [typeof(CreatePostUseCase).Assembly, typeof(CreateEventUseCase).Assembly],
    orchestrator => orchestrator.TreatAsRefusal<InvalidOperationException>());
```

It registers MediatR, every handler, processor and interceptor in those assemblies, the retry decorators, and these behaviours in order:
1. `PerformanceBehaviour` — logs slow requests (>500ms)
2. `ValidationBehaviour` — runs FluentValidation validators
3. `UnhandledExceptionBehaviour` — logs unhandled exceptions
4. Pre-processors, post-processors, exception actions and exception handlers

Handlers that implement `IRetry` are wrapped in their Polly policy; others are called directly.

**Startup fails if a request has no handler.** A hosted service checks every request type in the given assemblies and lists those with no registered handler, so a module left out of the list is caught at startup instead of on the first request.

### Domain Refusals

An aggregate refuses an operation by throwing `DomainRuleException`:

```csharp
if (Status == EventStatus.Draft)
    throw new DomainRuleException("Cannot record a result while the event is Draft");
```

The orchestrator returns it as a failed `OrchestratorResponse` carrying the message (`Succeeded == false`), which your use case reports as a 400. Any other exception propagates as a fault. Code that already signals rules with framework exceptions can opt them in with `TreatAsRefusal<T>()`.

When committing, `DbUpdateException` (constraint and concurrency conflicts) is also returned as a failed response; other exceptions propagate.

### Identity — JWT Authentication

```csharp
// AccountService issues JWTs
services.AddAccountDomain();  // includes Identity + JWT issuance

// Other services validate JWTs
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key512),
            TokenDecryptionKey = new SymmetricSecurityKey(key256),
        };
    });
```

## Module Dependency Graph

```
Kernel/Core
    └── Kernel/Application  (adds MediatR 12.5.0)
            └── Kernel/Infrastructure  (adds DI helpers)
DDD/Core
    └── DDD/Application      (extends Kernel/Application)
            └── DDD/Infrastructure
EventSourcing/Core
    └── EventSourcing/Application
            └── EventSourcing/Infrastructure
Identity/Core
    └── Identity/Application
            └── Identity/Infrastructure  (uses Kernel + DDD)
Persistence/Core
    └── Persistence/Application
            └── Persistence/Infrastructure
Email/Application
    └── Email/Infrastructure  (MailKit/MimeKit)
Jobs/Application
    └── Jobs/Infrastructure   (Hangfire)
```

## Adding Aviant to a New Project

1. Add as a git submodule (see above)
2. Add `Library/Aviant/Aviant.sln` or individual `.csproj` references to your solution
3. Reference the modules you need from your domain projects
4. Register via the `Add*` extension methods in your `Program.cs` or DI registration class

## Dependencies and Licensing

Aviant is MIT licensed and keeps its dependency tree free of commercial or copyleft terms, so using it never obliges you to buy a licence or publish your source.

- **MediatR is pinned to 12.5.0**, the last Apache-2.0 release. MediatR 13 and later need a commercial licence key; do not float the version upwards.
- **No object mapper is bundled.** AutoMapper 15 and later are RPL-1.5 or commercial, so the former `IMapFrom`/`IMapTo` helpers were removed. Map explicitly (a static `From(entity)` factory on the DTO) or use a source generator such as [Mapperly](https://github.com/riok/mapperly).
- **Tests use AwesomeAssertions**, the Apache-2.0 continuation of FluentAssertions (whose version 8 is non-commercial only).
- **Known vulnerabilities fail the build.** NuGet audit covers transitive packages and treats moderate, high and critical advisories (NU1902–NU1904) as errors.

## Contribution

Pull requests and issue reports are welcome at [github.com/panosru/Aviant](https://github.com/panosru/Aviant/issues).
