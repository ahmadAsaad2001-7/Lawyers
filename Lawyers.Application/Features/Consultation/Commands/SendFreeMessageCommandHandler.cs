using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Consultations.Commands;

public class SendFreeMessageCommandHandler: IRequestHandler<SendFreeMessageCommand,bool>
{
    private readonly IUnitOfWork _unitOfWork;
    public SendFreeMessageCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<bool> Handle(SendFreeMessageCommand request, CancellationToken cancellationToken)
    { // 
    //     var hasSentBefore = await _unitOfWork.FreeMessages.Query().AnyAsync(m => m.LawyerId == request.LawyerId && m.SenderIpAddress == request.IpAddress, cancellationToken);
    //     
    //     if (hasSentBefore)
    //     {
    //         // You can throw a custom exception here to catch in the controller
    //         throw new InvalidOperationException("You have already sent a free message to this lawyer.");
    //     }

        // 2. Map and Save the message
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

        return true;
    }
}