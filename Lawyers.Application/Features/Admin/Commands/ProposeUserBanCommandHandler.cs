using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Admin.Commands;

public class ProposeUserBanHandler : IRequestHandler<ProposeUserBanCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public ProposeUserBanHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<int> Handle(ProposeUserBanCommand request, CancellationToken ct)
    {
        if (!_currentUserService.IsAdmin)
            throw new UnauthorizedAccessException("Only admins can propose bans.");

        // Prevent banning other admins (optional safety check)
        var targetUser = await _unitOfWork.Users.Query().FirstOrDefaultAsync(u => u.Id == request.TargetUserId, ct);
        if (targetUser == null) throw new Exception("Target user not found.");
        if (targetUser.Role == Domain.Entities.Enums.Roles.Admin) throw new Exception("Cannot ban another admin.");

        var currentAdminId = _currentUserService.UserId!.Value;

        // Create the vote
        var vote = new AdminVote
        {
            ActionType = "BanUser",
            TargetUserId = request.TargetUserId,
            InitiatorAdminId = currentAdminId,
            Reason = request.Reason,
            ApprovalCount = 1, // Initiator automatically approves
            IsResolved = false
        };

        await _unitOfWork.AdminVotes.AddAsync(vote, ct);
        await _unitOfWork.SaveChangesAsync(ct); // Save to get the Vote ID

        // Record the initiator's participation
        var participant = new VoteParticipant
        {
            AdminVoteId = vote.Id,
            AdminUserId = currentAdminId,
            IsApproved = true
        };
        await _unitOfWork.VoteParticipants.AddAsync(participant, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return vote.Id;
    }
}