namespace Lawyers.Application.Interfaces;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string toEmail, string verificationLink);
    Task SendBookingConfirmationAsync(string toEmail, DateTime scheduledAt);
    Task SendPasswordResetEmailAsync(string toEmail, string resetLink);

}