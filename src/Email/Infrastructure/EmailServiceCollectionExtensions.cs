using Aviant.Application.Email;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Aviant.Infrastructure.Email;

public static class EmailServiceCollectionExtensions
{
    /// <summary>
    ///     Registers SMTP delivery: <see cref="IEmailSender" />, the fluent <see cref="IEmailService" />, and
    ///     <paramref name="settings" /> as the <see cref="IEmailSettingsSource" />.
    /// </summary>
    /// <remarks>
    ///     To choose the server per tenant or site, register your own <see cref="IEmailSettingsSource" /> (scoped) before
    ///     calling this. To send through an HTTP provider, register your own <see cref="IEmailSender" /> the same way;
    ///     it can take <see cref="SmtpEmailSender" /> to fall back to SMTP.
    /// </remarks>
    public static IServiceCollection AddAviantEmail(this IServiceCollection services, SmtpSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        services.TryAddSingleton<IEmailSettingsSource>(new StaticEmailSettingsSource(settings));
        services.TryAddScoped<SmtpEmailSender>();
        services.TryAddScoped<IEmailSender>(provider => provider.GetRequiredService<SmtpEmailSender>());
        services.TryAddScoped<IEmailService, EmailService>();

        return services;
    }

    private sealed class StaticEmailSettingsSource(SmtpSettings settings) : IEmailSettingsSource
    {
        public ValueTask<SmtpSettings> GetSmtpSettingsAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(settings);
    }
}
