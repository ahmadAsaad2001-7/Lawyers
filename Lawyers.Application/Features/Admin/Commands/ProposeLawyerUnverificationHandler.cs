using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Admin.Commands;

public class ProposeLawyerUnverificationHandler 
    : IRequestHandler<ProposeLawyerUnverificationCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public ProposeLawyerUnverificationHandler(
        IUnitOfWork unitOfWork, 
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<int> Handle(ProposeLawyerUnverificationCommand request, CancellationToken ct)
    {
        if (!_currentUserService.IsAdmin)
            throw new UnauthorizedAccessException("Only admins can propose unverifying.");

        // Verify the target is actually a verified lawyer
        var lawyerProfile = await _unitOfWork.LawyerProfiles.Query()
            .FirstOrDefaultAsync(lp => lp.UserId == request.LawyerUserId, ct);

        if (lawyerProfile == null)
            throw new InvalidOperationException("Target user is not a lawyer.");

        if (!lawyerProfile.IsVerified)
            throw new InvalidOperationException("Lawyer is already unverified.");

        var currentAdminId = _currentUserService.UserId!.Value;

        // Check for existing unresolved unverification votes
        var existingVote = await _unitOfWork.AdminVotes.Query()
            .AnyAsync(v => v.TargetUserId == request.LawyerUserId 
                        && v.ActionType == "UnverifyLawyer" 
                        && !v.IsResolved, ct);

        if (existingVote)
            throw new InvalidOperationException("There is already an active unverification vote for this lawyer.");

        // Create the vote
        var vote = new AdminVote
        {
            ActionType = "UnverifyLawyer",
            TargetUserId = request.LawyerUserId,
            InitiatorAdminId = currentAdminId,
            Reason = request.Reason,
            ApprovalCount = 1, // Initiator automatically approves
            IsResolved = false
        };

        await _unitOfWork.AdminVotes.AddAsync(vote, ct);
        await _unitOfWork.SaveChangesAsync(ct);

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
