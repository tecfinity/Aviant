namespace Aviant.Application.Identity;

public sealed record EmailConfirmationTicket(
    string Email,
    string FullName,
    string Token);
