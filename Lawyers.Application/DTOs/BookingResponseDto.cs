namespace Lawyers.Application.DTOs;

public class BookingResponseDto
{
    public int ConsultationId { get; set; }
    public int LawyerId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalCost { get; set; }
    public string PaymentClientSecret { get; set; } = string.Empty;
}