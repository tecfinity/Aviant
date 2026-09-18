# Changelog

All notable changes to Aviant are recorded here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow [Semantic Versioning](https://semver.org/). Versions are taken from git tags (`v1.2.3`).

## [Unreleased]

### Added
- `AddAviantCqrs(assemblies, configureOrchestrator)` registers the whole MediatR pipeline in one call: handlers, processors, interceptors, retry decorators and the ordered behaviours.
- Startup check (`CqrsHandlerValidator`): the application fails to start, naming the requests, when a request in a registered or referenced module has no handler.
- `DomainRuleException` and `OrchestratorOptions.TreatAsRefusal<T>()`: an aggregate's refusal becomes a failed `OrchestratorResponse` carrying its message instead of an unhandled exception.
- NuGet packages for every module, with README, SourceLink and symbol packages; versions from git tags via MinVer.
- GitHub Actions: CI (build, test, pack), tag-driven release, CodeQL on .NET 10; Dependabot.

### Changed
- **MediatR 11 → 12.5.0**, constrained to `[12.5.0, 13.0.0)`; 13+ is commercially licensed. `ICommandHandler<TCommand>` derives from `IRequestHandler<TCommand, Unit>`, which is what the single-arity interface meant in MediatR 11.
- The persistence orchestrator's commit step returns only `DbUpdateException` and refusals as failed responses. Other exceptions propagate instead of being reported to the caller as validation messages.
- Domain configuration files rank directly above the host's `appsettings*` files and below everything added after them: user secrets, environment variables, command line, key vaults.
- Known vulnerable packages (NU1902–NU1904) fail the build, including transitive ones.
- MailKit/MimeKit 4.18.0, BouncyCastle 2.7.0; `Microsoft.AspNetCore.Http.Abstractions` 2.2 replaced by the ASP.NET Core framework reference.
- Tests use AwesomeAssertions instead of FluentAssertions.

### Removed
- **Breaking:** `Aviant.Application.Mappings` (`IMapFrom`, `IMapTo`, `MappingProfile`) and the AutoMapper dependency, which is RPL-1.5 or commercial from v15. Map explicitly or use Mapperly.
- Unused `ExpressionCombiner` and `PredicateOperator`.

### Fixed
- `RetryRequestProcessor` / `RetryEventProcessor` threw `NullReferenceException` for handlers without a retry policy.
- The audit change tracker threw for `Unchanged` and `Detached` entities, which made any save fail while an audited entity was merely loaded.

## [1.0.0-preview.7] - 2020-10-18

Last tagged preview before this changelog was started.
