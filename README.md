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

### MediatR Pipeline Behaviours

Aviant registers these behaviours automatically (in order):
1. `PerformanceBehaviour` — logs slow requests (>500ms)
2. `ValidationBehaviour` — runs FluentValidation validators
3. `UnhandledExceptionBehaviour` — logs unhandled exceptions
4. `RetryRequestProcessor` — wraps handlers with Polly retry

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
    └── Kernel/Application  (adds MediatR)
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

## Contribution

Pull requests and issue reports are welcome at [github.com/panosru/Aviant](https://github.com/panosru/Aviant/issues).
