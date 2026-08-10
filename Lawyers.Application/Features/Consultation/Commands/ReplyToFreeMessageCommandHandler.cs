using Lawyers.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Consultations.Commands;

public class ReplyToFreeMessageCommandHandler : IRequestHandler<ReplyToFreeMessageCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    // private readonly IEmailService _emailService; 

    public ReplyToFreeMessageCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(ReplyToFreeMessageCommand request, CancellationToken cancellationToken)
    {
        var message = await _unitOfWork.FreeMessages.GetByIdAsync(request.MessageId, cancellationToken);
        if (message == null) throw new Exception("Message not found");

        // Security check: Ensure the logged-in user is the lawyer who owns this message
        var lawyer = await _unitOfWork.LawyerProfiles.Query()
            .FirstOrDefaultAsync(l => l.UserId == _currentUserService.UserId && l.Id == message.LawyerId, cancellationToken);
            
        if (lawyer == null) throw new UnauthorizedAccessException("You can only reply to messages sent to you.");

        // await _emailService.SendEmailAsync(message.SenderEmail, $"رد على استشارتك القانونية", request.ReplyContent);

        // Mark as replied in DB
        message.IsRepliedTo = true;
        _unitOfWork.FreeMessages.Update(message);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}