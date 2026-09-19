namespace Lawyers.Application.DTOs;

public record NotificationDto(int Id, string Title, string Message, bool IsRead, DateTime CreatedAt);

