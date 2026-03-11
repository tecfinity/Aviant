using MailKit.Net.Smtp;
using MailKit.Security;

namespace Aviant.Infrastructure.Email;

public class SmtpClientFactory : ISmtpClientFactory
{
    private readonly string _smtpHost;

    private readonly int _port;
    
    private readonly bool _useSsl;

    private readonly string _username;
    
    private readonly string _password;

    public SmtpClientFactory(
        string smtpHost,
        int    port,
        bool   useSsl,
        string username,
        string password)
    {
        _smtpHost  = smtpHost;
        _port     = port;
        _useSsl   = useSsl;
        _username = username;
        _password = password;
    }

    #region ISmtpClientFactory Members

    public SmtpClient GetSmtpClient()
    {
        SmtpClient client = new();

        client.Connect(
            _smtpHost,
            _port,
            _useSsl
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.None);

        if (!string.IsNullOrWhiteSpace(_username) && !string.IsNullOrWhiteSpace(_password))
            client.Authenticate(_username, _password);

        return client;
    }

    #endregion
}
