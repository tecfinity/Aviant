using System.Reflection;
using Aviant.Application.Behaviours;
using Aviant.Application.Interceptors;
using Aviant.Application.Orchestration;
using Aviant.Application.Processors;
using MediatR;
using MediatR.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Aviant.Application.Extensions;

/// <summary>
///     Registers the Aviant CQRS pipeline.
/// </summary>
public static class CqrsServiceCollectionExtensions
{
    /// <summary>
    ///     Registers MediatR, every handler, processor and interceptor found in
    ///     <paramref name="assemblies" /> (and in Aviant itself), the retry decorators and the
    ///     pipeline behaviours, in this order: performance, validation, unhandled exception
    ///     logging, pre-processors, post-processors, exception actions, exception handlers.
    /// </summary>
    /// <remarks>
    ///     Also registers a hosted service that fails application startup if a request type
    ///     in <paramref name="assemblies" /> has no handler. Without it, a module left out of
    ///     the assembly list fails only when one of its requests is first sent.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies holding requests and their handlers.</param>
    /// <param name="configureOrchestrator">Optional orchestrator configuration, e.g. extra refusal types.</param>
    public static IServiceCollection AddAviantCqrs(
        this IServiceCollection      services,
        IEnumerable<Assembly>        assemblies,
        Action<OrchestratorOptions>? configureOrchestrator = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        Assembly[] requestAssemblies = assemblies.Distinct().ToArray();
        Assembly[] scanned           = requestAssemblies.Append(typeof(LoggerBehaviour<>).Assembly).Distinct().ToArray();

        services.TryAddTransient<IMediator, Mediator>();
        services.TryAddTransient<ISender>(provider => provider.GetRequiredService<IMediator>());
        services.TryAddTransient<IPublisher>(provider => provider.GetRequiredService<IMediator>());

        services.Scan(
            scan => scan.FromAssemblies(scanned)
               .RegisterHandlers(typeof(IRequestHandler<>))
               .RegisterHandlers(typeof(IRequestHandler<,>))
               .RegisterHandlers(typeof(InterceptorBase<>))
               .RegisterHandlers(typeof(INotificationHandler<>))
               .RegisterHandlers(typeof(MediatR.Pipeline.IRequestPreProcessor<>))
               .RegisterHandlers(typeof(MediatR.Pipeline.IRequestPostProcessor<,>))
               .RegisterHandlers(typeof(IRequestExceptionHandler<,,>))
               .RegisterHandlers(typeof(IRequestExceptionAction<,>)));

        // TryDecorate: an application may have no handlers of one kind yet.
        services.TryDecorate(typeof(IRequestHandler<,>), typeof(RetryRequestProcessor<,>));
        services.TryDecorate(typeof(INotificationHandler<>), typeof(RetryEventProcessor<>));

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestPreProcessorBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestPostProcessorBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestExceptionActionProcessorBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestExceptionProcessorBehavior<,>));

        services.AddOptions<OrchestratorOptions>()
           .Configure(options => configureOrchestrator?.Invoke(options));

        services.AddSingleton<IHostedService>(
            provider => new CqrsHandlerValidator(
                requestAssemblies,
                provider.GetRequiredService<IServiceProviderIsService>()));

        return services;
    }
}
