using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Consultations.Commands;
public class SendFreeMessageCommandHandler : IRequestHandler<SendFreeMessageCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService; // ✅ new

    public SendFreeMessageCommandHandler(IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task<bool> Handle(SendFreeMessageCommand request, CancellationToken cancellationToken)
    {
        var message = new FreeConsultationMessage
        {
            LawyerId = request.LawyerId,
            SenderName = request.Name,
            SenderPhone = request.Phone,
            SenderEmail = request.Email,
            Content = request.Content,
            SenderIpAddress = request.IpAddress
        };

        await _unitOfWork.FreeMessages.AddAsync(message, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // ✅ Notify the lawyer — a potential client is reaching out and
        // they'd otherwise have no idea unless they happen to check.
        var lawyerProfile = await _unitOfWork.LawyerProfiles.GetByIdAsync(request.LawyerId, cancellationToken);
        if (lawyerProfile != null)
        {
            await _notificationService.NotifyAsync(
                lawyerProfile.UserId,
                "استفسار جديد",
                $"{request.Name} أرسل لك رسالة: \"{Truncate(request.Content, 80)}\"",
                cancellationToken);
        }

        return true;
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength] + "...";
}