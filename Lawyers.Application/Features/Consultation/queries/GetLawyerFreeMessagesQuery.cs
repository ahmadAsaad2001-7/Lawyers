using Lawyers.Application.DTOs;
using Lawyers.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Consultations.Queries;

public record GetLawyerFreeMessagesQuery : IRequest<List<FreeMessageDto>>;

public class GetLawyerFreeMessagesQueryHandler : IRequestHandler<GetLawyerFreeMessagesQuery, List<FreeMessageDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetLawyerFreeMessagesQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<List<FreeMessageDto>> Handle(GetLawyerFreeMessagesQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        // Find the Lawyer Profile associated with the logged-in User
        var lawyer = await _unitOfWork.LawyerProfiles.Query()
            .FirstOrDefaultAsync(l => l.UserId == userId, cancellationToken);

        if (lawyer == null)
        {
            return new List<FreeMessageDto>(); // Or throw an Unauthorized/NotFound exception
        }

        // Fetch all anonymous/free messages directed to this lawyer
        var messages = await _unitOfWork.FreeMessages.Query()
            .Where(m => m.LawyerId == lawyer.Id)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new FreeMessageDto
            {
                Id = m.Id,
                SenderName = m.SenderName,
                SenderPhone = m.SenderPhone,
                SenderEmail = m.SenderEmail,
                Content = m.Content,
                CreatedAt = m.CreatedAt,
                IsRepliedTo = m.IsRepliedTo
            })
            .ToListAsync(cancellationToken);

        return messages;
    }
}