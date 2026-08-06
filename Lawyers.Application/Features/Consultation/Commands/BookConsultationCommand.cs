using Lawyers.Application.DTOs;
using Lawyers.Application.Enums;
using MediatR;

namespace Lawyers.Application.Features.Consultations.Commands;

public class BookConsultationCommand : IRequest<BookingResponseDto>
{
    public int LawyerId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = 60; // Default to 1 hour
    
    public PaymentChannel Channel { get; set; } = PaymentChannel.Card;
        public string? CardToken { get; set; }
        public string? WalletPhoneNumber { get; set; }
}