using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Admin.Commands;

public class ProposeLawyerVerificationHandler : IRequestHandler<ProposeLawyerVerificationCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public ProposeLawyerVerificationHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<int> Handle(ProposeLawyerVerificationCommand request, CancellationToken ct)
    {
        if (!_currentUserService.IsAdmin)
            throw new UnauthorizedAccessException("Only admins can propose verification.");

        var lawyerProfile = await _unitOfWork.LawyerProfiles.Query()
            .FirstOrDefaultAsync(lp => lp.UserId == request.LawyerUserId, ct);
        
        if (lawyerProfile == null)
            throw new InvalidOperationException("Target user is not a lawyer.");
        
        if (lawyerProfile.IsVerified)
            throw new InvalidOperationException("Lawyer is already verified.");

        var hasActiveVote = await _unitOfWork.AdminVotes.Query()
            .AnyAsync(vote => vote.TargetUserId == request.LawyerUserId &&
                              vote.ActionType == "VerifyLawyer" &&
                              !vote.IsResolved, ct);
        if (hasActiveVote)
            throw new InvalidOperationException("There is already an active verification vote for this lawyer.");

        var currentAdminId = _currentUserService.UserId!.Value;

        var vote = new AdminVote
        {
            ActionType = "VerifyLawyer",
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
