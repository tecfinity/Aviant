using System.Reflection;
using Aviant.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Aviant.Application.Extensions;

/// <summary>
///     Registers use cases so they receive the dependency scope they run in.
/// </summary>
public static class UseCaseServiceCollectionExtensions
{
    /// <summary>
    ///     Registers every concrete use case in <paramref name="assemblies" /> as scoped. Each one is
    ///     constructed with its constructor dependencies and then activated with the scope's
    ///     provider, from which it resolves its orchestrator and validators. That works wherever a
    ///     scope exists (a request, a background job, a test), not only inside an HTTP request.
    /// </summary>
    /// <remarks>An existing registration of the same use case is replaced.</remarks>
    public static IServiceCollection AddAviantUseCases(this IServiceCollection services, IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        IEnumerable<Type> useCases = assemblies
           .Distinct()
           .SelectMany(LoadableTypes)
           .Where(
                type => type is { IsClass: true, IsAbstract: false, ContainsGenericParameters: false }
                     && typeof(IUseCaseActivation).IsAssignableFrom(type));

        foreach (var useCase in useCases)
        {
            services.RemoveAll(useCase);
            services.AddScoped(
                useCase,
                provider =>
                {
                    var instance = (IUseCaseActivation)ActivatorUtilities.CreateInstance(provider, useCase);
                    instance.Activate(provider);

                    return instance;
                });
        }

        return services;
    }

    private static IEnumerable<Type> LoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
    }
}
