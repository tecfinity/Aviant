# Changelog

All notable changes to Aviant are recorded here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow [Semantic Versioning](https://semver.org/). Versions are taken from git tags (`v1.2.3`).

## [2.0.0] - Unreleased

Aviant 2 removes the static service locator, makes persistence truly asynchronous, moves event storage to the supported KurrentDB client, and stops tying the library to Serilog. See [Migrating from 1.x](#migrating-from-1x) below.

### Added
- `AddAviantUseCases(assemblies)` registers every use case with a factory that activates it with its own scope's services, so use cases work in background jobs and workers as well as in HTTP requests.
- `Repository<TDbContext, TEntity, TKey>`: one repository base for both reads and writes against a single context.
- `AddKurrentDb(connectionString)` registers the KurrentDB gRPC client. Accepts `kurrentdb://` and `esdb://` connection strings.
- `Clock.TimeProvider`: every clock provider reads time from a replaceable `TimeProvider`.
- Integration tests for the events repository against a real KurrentDB container.
- `AddAviantCqrs(assemblies, configureOrchestrator)` registers the whole MediatR pipeline in one call: handlers, processors, interceptors, retry decorators and the ordered behaviours.
- Startup check (`CqrsHandlerValidator`): the application fails to start, naming the requests, when a request in a registered or referenced module has no handler.
- `DomainRuleException` and `OrchestratorOptions.TreatAsRefusal<T>()`: an aggregate's refusal becomes a failed `OrchestratorResponse` carrying its message instead of an unhandled exception.
- NuGet packages for every module, with README, SourceLink and symbol packages; versions from git tags via MinVer.
- GitHub Actions: CI (build, test, pack), tag-driven release, CodeQL on .NET 10; Dependabot.

### Changed
- **Event storage uses `KurrentDB.Client` 1.4 (gRPC)** instead of the legacy TCP client `EventStore.Client` 22, which KurrentDB and EventStoreDB 24+ no longer serve. A write with a stale expected version throws `WrongExpectedVersionException`.
- **Repositories are async-only.** Reads run real EF Core async queries with cancellation. Writes call `ValidateAsync` and refuse an invalid entity with `DomainRuleException`, where before its result was ignored. `UpdateAsync` keeps change tracking, so only changed columns are written.
- Repositories and `UnitOfWork` no longer dispose the context they were given; the DI scope owns it.
- Event-sourced `CommandHandler` takes `IEventsService` in its constructor.
- Auditing reads `ICurrentUserService` from the context's own scope and stamps `Guid.Empty` when there is no user, as in background work.
- Behaviours and the Kafka producer and consumer log through `ILogger<T>` with `[LoggerMessage]`. Requests are logged by name only; their contents, passwords included, were logged before.
- **MediatR 11 → 12.5.0**, constrained to `[12.5.0, 13.0.0)`; 13+ is commercially licensed. `ICommandHandler<TCommand>` derives from `IRequestHandler<TCommand, Unit>`, which is what the single-arity interface meant in MediatR 11.
- The persistence orchestrator's commit step returns only `DbUpdateException` and refusals as failed responses. Other exceptions propagate instead of being reported to the caller as validation messages.
- Domain configuration files rank directly above the host's `appsettings*` files and below everything added after them: user secrets, environment variables, command line, key vaults.
- Known vulnerable packages (NU1902–NU1904) fail the build, including transitive ones.
- MailKit/MimeKit 4.18.0, BouncyCastle 2.7.0; `Microsoft.AspNetCore.Http.Abstractions` 2.2 replaced by the ASP.NET Core framework reference.
- Tests use AwesomeAssertions instead of FluentAssertions.

### Removed
- **Breaking:** `ServiceLocator`, `IServiceContainer`, `HttpContextServiceProviderProxy`, `MessagesFacade` and `AssertionsConcernValidator`.
- **Breaking:** the synchronous `Insert`, `Update` and `Delete` repository methods; repositories are no longer `IDisposable`.
- **Breaking:** `EventStoreConnectionWrapper` and `IEventStoreConnectionWrapper`.
- **Breaking:** the Serilog dependency (`Serilog.AspNetCore`, `Serilog.Settings.Configuration`) and `PerformanceBehaviour.Timer`.
- **Breaking:** `Aviant.Application.Mappings` (`IMapFrom`, `IMapTo`, `MappingProfile`) and the AutoMapper dependency, which is RPL-1.5 or commercial from v15. Map explicitly or use Mapperly.
- Unused `ExpressionCombiner` and `PredicateOperator`.

### Fixed
- An entity whose `ValidateAsync` returned `false` was saved anyway.
- The Identity `PerformanceBehaviour` hid `Handle` instead of overriding it, so it never ran.
- `RetryRequestProcessor` / `RetryEventProcessor` threw `NullReferenceException` for handlers without a retry policy.
- The audit change tracker threw for `Unchanged` and `Detached` entities, which made any save fail while an audited entity was merely loaded.

### Migrating from 1.x

1. **Register use cases** with `services.AddAviantUseCases(assemblies)`, passing the same assemblies you give `AddAviantCqrs`. Remove every `AddScoped<SomeUseCase>()` and every `ServiceLocator.Initialise(...)` / `IServiceContainer` registration.
2. **Event-sourced command handlers** pass the events service to the base: `public MyHandler(IEventsService<A, AId> events) : base(events)`.
3. **Repositories:** replace `Insert`/`Update`/`Delete` with their `...Async` versions, and stop disposing repositories. Check that `ValidateAsync` on your entities returns `true` for valid state; a `false` now blocks the write.
4. **Event store:** replace
   ```csharp
   services.AddSingleton<IEventStoreConnectionWrapper>(_ => new EventStoreConnectionWrapper(new Uri(connectionString)));
   ```
   with `services.AddKurrentDb(connectionString)`, and use a gRPC connection string such as `esdb://admin:changeit@host:2113?tls=false`. The server must expose gRPC (EventStoreDB 20.10 or later, or KurrentDB); existing streams are read as before.
5. **Logging:** hosts that use Serilog reference `Serilog.AspNetCore` themselves and call `UseSerilog()`; Aviant's log entries reach it through `ILogger<T>`. Subclasses of `LoggerBehaviour` or `PerformanceBehaviour` take an `ILogger<T>` in their constructor, and slow-request handling overrides `OnLongRunningAsync`.
6. **Tests that control time** set `Clock.TimeProvider` to a fake instead of writing a custom `IClockProvider`.

## [1.0.0-preview.7] - 2020-10-18

Last tagged preview before this changelog was started.
