using System.Reflection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aviant.Application.Extensions;

/// <summary>
///     Fails application startup when a request type has no registered handler.
/// </summary>
public sealed class CqrsHandlerValidator : IHostedService
{
    private readonly IReadOnlyCollection<Assembly> _assemblies;

    private readonly IServiceProviderIsService _isService;

    public CqrsHandlerValidator(IReadOnlyCollection<Assembly> assemblies, IServiceProviderIsService isService)
    {
        _assemblies = assemblies;
        _isService  = isService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        List<string> unhandled = _assemblies
           .SelectMany(LoadableTypes)
           .Where(type => type is { IsClass: true, IsAbstract: false, ContainsGenericParameters: false })
           .SelectMany(
                type => type.GetInterfaces()
                   .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>))
                   .Select(i => (Request: type, Response: i.GetGenericArguments()[0])))
           .Where(
                pair => !_isService.IsService(
                    typeof(IRequestHandler<,>).MakeGenericType(pair.Request, pair.Response)))
           .Select(pair => pair.Request.FullName ?? pair.Request.Name)
           .Distinct()
           .Order(StringComparer.Ordinal)
           .ToList();

        if (unhandled.Count > 0)
            throw new InvalidOperationException(
                "These requests have no registered handler. Add the assembly holding the handler to "
              + "AddAviantCqrs, or check that the handler is public and not abstract:"
              + Environment.NewLine
              + string.Join(Environment.NewLine, unhandled.Select(name => "  - " + name)));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

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
