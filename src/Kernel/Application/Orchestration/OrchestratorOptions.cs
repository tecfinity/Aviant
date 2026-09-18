using Aviant.Core.Exceptions;

namespace Aviant.Application.Orchestration;

/// <summary>
///     Options for the orchestrators.
/// </summary>
public sealed class OrchestratorOptions
{
    private readonly HashSet<Type> _refusalExceptionTypes = [typeof(DomainRuleException)];

    /// <summary>
    ///     Exception types that mean "the domain refused this request" rather than
    ///     "something broke". The orchestrator returns them as a failed response with the
    ///     exception message; any other exception propagates. Subclasses match too.
    ///     <see cref="DomainRuleException" /> is always included.
    /// </summary>
    public IReadOnlyCollection<Type> RefusalExceptionTypes => _refusalExceptionTypes;

    /// <summary>
    ///     Treats <typeparamref name="TException" /> (and its subclasses) as a refusal.
    ///     Useful when existing aggregates signal rule violations with framework
    ///     exceptions such as <see cref="InvalidOperationException" />.
    /// </summary>
    public OrchestratorOptions TreatAsRefusal<TException>()
        where TException : Exception
    {
        _refusalExceptionTypes.Add(typeof(TException));

        return this;
    }

    /// <summary>
    ///     Whether <paramref name="exception" /> is a refusal.
    /// </summary>
    public bool IsRefusal(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return _refusalExceptionTypes.Any(type => type.IsInstanceOfType(exception));
    }
}
