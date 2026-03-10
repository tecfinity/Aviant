namespace Aviant.Application.Identity;

public sealed record EmailChangeTicket(
    string CurrentEmail,
    string NewEmail,
    string FullName,
    string Token);
