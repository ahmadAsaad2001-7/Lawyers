using Lawyers.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;

namespace Lawyers.InfraStructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;
    public SmtpEmailService(IConfiguration config)
    {
        _config = config;
        _logger = new LoggerFactory().CreateLogger<SmtpEmailService>();
    }

    public async Task SendVerificationEmailAsync(string toEmail, string verificationLink)
    {
        var htmlBody = $@"
            <div style='font-family: Arial, sans-serif; padding: 20px;'>
                <h2 style='color: #065f46;'>Welcome to Lawyers Platform!</h2>
                <p>Please click the button below to verify your email address:</p>
                <a href='{verificationLink}' style='background-color: #f59e0b; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>
                    Verify Email
                </a>
                <p style='margin-top: 20px; color: gray;'>If you didn't create an account, please ignore this email.</p>
            </div>";

        await SendEmailAsync(toEmail, "Verify your email address", htmlBody);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
    {
        var htmlBody = $@"
            <div style='font-family: Arial, sans-serif; padding: 20px; color: #333;'>
                <h2 style='color: #b91c1c;'>Reset Your Password</h2>
                <p>We received a request to reset your password for your Lawyers Platform account.</p>
                <p>Click the button below to choose a new password. This link will expire in 24 hours.</p>
                <a href='{resetLink}' style='background-color: #b91c1c; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block; margin: 15px 0; font-weight: bold;'>
                    Reset Password
                </a>
                <p style='margin-top: 30px; color: #666; font-size: 12px;'>
                    If you didn't request this, please ignore this email. Your password will remain unchanged.
                </p>
            </div>";

        await SendEmailAsync(toEmail, "Reset Your Password", htmlBody);
    }

    public async Task SendBookingConfirmationAsync(string toEmail, DateTime scheduledAt)
    {
        var htmlBody = $@"
            <div style='font-family: Arial, sans-serif; padding: 20px;'>
                <h2 style='color: #065f46;'>Booking Confirmed!</h2>
                <p>Your consultation has been successfully scheduled and paid for.</p>
                <p><strong>Scheduled Time:</strong> {scheduledAt:MMMM dd, yyyy 'at' hh:mm tt}</p>
                <p>You will receive a link to join the session shortly before the scheduled time.</p>
            </div>";

        await SendEmailAsync(toEmail, "Booking Confirmation", htmlBody);
    }

    // ✅ Centralized, robust email sending logic
    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        var smtpHost = _config["Smtp:Host"] ?? "127.0.0.1";
        var smtpPort = int.Parse(_config["Smtp:Port"] ?? "25");
        var smtpUser = _config["Smtp:Username"];
        var smtpPass = _config["Smtp:Password"];
        var enableSsl = bool.TryParse(_config["Smtp:EnableSsl"], out var ssl) ? ssl : false;

        var fromAddress = string.IsNullOrWhiteSpace(smtpUser) ? "noreply@lawyersplatform.com" : smtpUser;

        _logger.LogInformation(
            "[SmtpEmailService] Sending email. Host={Host}, Port={Port}, HasUser={HasUser}, EnableSsl={EnableSsl}, From={From}, To={To}",
            smtpHost, smtpPort, !string.IsNullOrWhiteSpace(smtpUser), enableSsl, fromAddress, toEmail);

        using var client = new SmtpClient(smtpHost, smtpPort) { EnableSsl = enableSsl };

        if (!string.IsNullOrWhiteSpace(smtpUser))
        {
            client.Credentials = new NetworkCredential(smtpUser, smtpPass);
        }

        var mailMessage = new MailMessage
        {
            From = new MailAddress(fromAddress, "Lawyers Platform"),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        mailMessage.To.Add(toEmail);

        try
        {
            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("[SmtpEmailService] Email sent successfully to {To}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SmtpEmailService] Failed to send email to {To}", toEmail);
            throw;
        }
    }
}