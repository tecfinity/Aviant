namespace Aviant.Application.Identity;

public interface IIdentityService
{
    public Task<object?> AuthenticateAsync(
        string            username,
        string            password,
        string?           twoFactorCode = null,
        string?           recoveryCode = null,
        CancellationToken cancellationToken = default);

    public Task<MfaSetupTicket?> BeginMfaSetupAsync(
        Guid              userId,
        CancellationToken cancellationToken = default);

    public Task<MfaRecoveryCodesTicket?> EnableMfaAsync(
        Guid              userId,
        string            code,
        CancellationToken cancellationToken = default);

    public Task<IdentityResult> DisableMfaAsync(
        Guid              userId,
        string            password,
        CancellationToken cancellationToken = default);

    public Task<MfaRecoveryCodesTicket?> RegenerateRecoveryCodesAsync(
        Guid              userId,
        string            password,
        CancellationToken cancellationToken = default);

    public Task<EmailConfirmationTicket?> GenerateEmailConfirmationAsync(
        string            email,
        CancellationToken cancellationToken = default);

    public Task<EmailChangeTicket?> GenerateEmailChangeAsync(
        Guid              userId,
        string            newEmail,
        CancellationToken cancellationToken = default);

    public Task<PasswordResetTicket?> GeneratePasswordResetAsync(
        string            email,
        CancellationToken cancellationToken = default);

    public Task<IdentityResult> ResetPasswordAsync(
        string            email,
        string            token,
        string            newPassword,
        CancellationToken cancellationToken = default);

    public Task<IdentityResult> ChangePasswordAsync(
        Guid              userId,
        string            currentPassword,
        string            newPassword,
        CancellationToken cancellationToken = default);

    public Task<IdentityResult> ConfirmEmailAsync(
        string            token,
        string            email,
        CancellationToken cancellationToken = default);

    public Task<IdentityResult> ConfirmEmailChangeAsync(
        string            currentEmail,
        string            newEmail,
        string            token,
        CancellationToken cancellationToken = default);

    public Task<string> GetUserNameAsync(
        Guid              userId,
        CancellationToken cancellationToken = default);

    public Task<Guid?> GetUserIdByEmailAsync(
        string            email,
        CancellationToken cancellationToken = default);

    public Task<(IdentityResult Result, Guid UserId)> CreateUserAsync(
        string            username,
        string            password,
        string            firstName,
        string            lastName,
        IEnumerable<string> roles,
        bool              emailConfirmed,
        CancellationToken cancellationToken = default);

    public Task<IdentityResult> DeleteUserAsync(
        Guid              userId,
        CancellationToken cancellationToken = default);
}
