namespace Lawyers.Application.Features.Admin.Commands;

public class AdminDeletePostRequest
{
    public string Reason { get; set; } = string.Empty; // e.g., "Spam", "Inappropriate content", "Violates terms"
}