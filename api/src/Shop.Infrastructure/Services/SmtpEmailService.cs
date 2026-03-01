using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using Shop.Application.Common.Interfaces;

namespace Shop.Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var smtpHost = _config["Email:SmtpHost"];
        if (string.IsNullOrEmpty(smtpHost))
        {
            _logger.LogWarning("Email SMTP not configured. Skipping email to {To}: {Subject}", to, subject);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _config["Email:FromName"] ?? "Shop",
            _config["Email:FromAddress"] ?? "noreply@shop.com"));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            var port = int.Parse(_config["Email:SmtpPort"] ?? "587");
            await client.ConnectAsync(smtpHost, port, SecureSocketOptions.StartTls, ct);

            var username = _config["Email:Username"];
            if (!string.IsNullOrEmpty(username))
            {
                await client.AuthenticateAsync(username, _config["Email:Password"], ct);
            }

            await client.SendAsync(message, ct);
            _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}: {Subject}", to, subject);
        }
        finally
        {
            await client.DisconnectAsync(true, ct);
        }
    }
}
