namespace Aviant.Core.Exceptions;

/// <summary>
///     Thrown by an aggregate or entity to refuse an operation that breaks a domain rule.
/// </summary>
/// <remarks>
///     A refusal is not a fault: its message explains the rule to the caller
///     ("Cannot record a result while the event is Draft"). The orchestrator turns it
///     into a failed <c>OrchestratorResponse</c> carrying the message instead of letting
///     it escape as an unhandled exception.
/// </remarks>
public class DomainRuleException : CoreException
{
    public DomainRuleException(string message)
        : base(message)
    { }

    public DomainRuleException(string message, Exception inner)
        : base(message, inner)
    { }
}
