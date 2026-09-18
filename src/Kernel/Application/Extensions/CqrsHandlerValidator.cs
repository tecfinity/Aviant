using System.Reflection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aviant.Application.Extensions;

/// <summary>
///     Fails application startup when a request type has no registered handler.
/// </summary>
/// <remarks>
///     Checks the requests in the registered assemblies and, when a root assembly is given
///     (the host's entry assembly), in every assembly it references directly or
///     transitively that builds on MediatR or Aviant. The second part is what catches a
///     module the host references but forgot to register: its requests are not in the
///     registered assemblies, so without it nothing would look at them.
/// </remarks>
public sealed class CqrsHandlerValidator : IHostedService
{
    private static readonly string[] RequestDefiningDependencies = ["MediatR", "MediatR.Contracts", "Aviant.Application"];

    private readonly IReadOnlyCollection<Assembly> _assemblies;

    private readonly IServiceProviderIsService _isService;

    private readonly Assembly? _rootAssembly;

    public CqrsHandlerValidator(
        IReadOnlyCollection<Assembly> assemblies,
        IServiceProviderIsService     isService,
        Assembly?                     rootAssembly = null)
    {
        _assemblies   = assemblies;
        _isService    = isService;
        _rootAssembly = rootAssembly;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        List<string> unhandled = AssembliesToCheck()
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
                "These requests have no registered handler. Add the assembly holding them and their "
              + "handlers to AddAviantCqrs, or check that the handler is public and not abstract:"
              + Environment.NewLine
              + string.Join(Environment.NewLine, unhandled.Select(name => "  - " + name)));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private IEnumerable<Assembly> AssembliesToCheck()
    {
        HashSet<Assembly> result = [.. _assemblies];

        if (_rootAssembly is null)
            return result;

        HashSet<string>  visited = new(StringComparer.Ordinal);
        Queue<Assembly> pending = new([_rootAssembly]);

        while (pending.TryDequeue(out var assembly))
        {
            if (!visited.Add(assembly.FullName ?? assembly.GetName().Name ?? string.Empty))
                continue;

            AssemblyName[] references = assembly.GetReferencedAssemblies();

            if (references.Any(reference => RequestDefiningDependencies.Contains(reference.Name)))
                result.Add(assembly);

            foreach (var reference in references.Where(IsCandidate))
                if (TryLoad(reference) is { } loaded)
                    pending.Enqueue(loaded);
        }

        return result;
    }

    private static bool IsCandidate(AssemblyName name) =>
        name.Name is { } simpleName
     && !simpleName.StartsWith("System", StringComparison.Ordinal)
     && !simpleName.StartsWith("Microsoft", StringComparison.Ordinal)
     && simpleName is not ("netstandard" or "mscorlib");

    private static Assembly? TryLoad(AssemblyName name)
    {
        try
        {
            return Assembly.Load(name);
        }
        catch (Exception exception) when (exception is FileNotFoundException or FileLoadException or BadImageFormatException)
        {
            return null;
        }
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
