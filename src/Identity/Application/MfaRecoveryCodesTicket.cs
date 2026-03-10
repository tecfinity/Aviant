namespace Aviant.Application.Identity;

public sealed record MfaRecoveryCodesTicket(
    IReadOnlyCollection<string> RecoveryCodes);
