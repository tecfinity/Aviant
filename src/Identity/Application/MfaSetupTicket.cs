namespace Aviant.Application.Identity;

public sealed record MfaSetupTicket(
    string SharedKey,
    string AuthenticatorUri,
    bool IsEnabled);
