namespace Aviant.Application.Identity;

public sealed record PasswordResetTicket(
    Guid UserId,
    string Email,
    string FullName,
    string Token);
