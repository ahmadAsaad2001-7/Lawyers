using Lawyers.Application.DTOs;
using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities;
using Lawyers.Domain.Entities.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Admin.Commands;

public class CastAdminVoteHandler : IRequestHandler<CastAdminVoteCommand, CastVoteResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly UserManager<User> _userManager;
    private readonly INotificationService _notificationService; // ✅ new

    private const int APPROVAL_THRESHOLD = 2;
    private const int DISAPPROVAL_THRESHOLD = 2; // ✅ new

    public CastAdminVoteHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        UserManager<User> userManager,
        INotificationService notificationService) // ✅ new
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _userManager = userManager;
        _notificationService = notificationService;
    }

    public async Task<CastVoteResponse> Handle(CastAdminVoteCommand request, CancellationToken ct)
    {
        var currentAdminId = _currentUserService.UserId!.Value;

        var vote = await _unitOfWork.AdminVotes.Query()
            .Include(v => v.TargetUser)
            .FirstOrDefaultAsync(v => v.Id == request.VoteId, ct);

        if (vote == null || vote.IsResolved)
            throw new InvalidOperationException("Vote not found or already finalized.");

        var alreadyVoted = await _unitOfWork.VoteParticipants.Query()
            .AnyAsync(p => p.AdminVoteId == request.VoteId && p.AdminUserId == currentAdminId, ct);

        if (alreadyVoted)
            throw new InvalidOperationException("You have already voted on this decision.");

        if (request.IsApproved) vote.ApprovalCount++;
        else vote.DisapprovalCount++;

        var participant = new VoteParticipant
        {
            AdminVoteId = request.VoteId,
            AdminUserId = currentAdminId,
            IsApproved = request.IsApproved
        };
        await _unitOfWork.VoteParticipants.AddAsync(participant, ct);

        bool finalized = false;
     
        if (vote.ApprovalCount >= APPROVAL_THRESHOLD)
        {
            finalized = true;
            vote.IsResolved = true;
            await ExecuteVoteAction(vote, ct);

            if (vote.InitiatorAdminId.HasValue && vote.InitiatorAdminId.Value != currentAdminId)
            {
                await _notificationService.NotifyAsync(vote.InitiatorAdminId.Value, "Proposal resolved",
                    $"Your {vote.ActionType} proposal for user #{vote.TargetUserId} has been approved and applied.", ct);
            }
        }
        else if (vote.DisapprovalCount >= DISAPPROVAL_THRESHOLD)
        {
            finalized = true;
            vote.IsResolved = true;
            // No ExecuteVoteAction — nothing to apply on rejection.

            if (vote.InitiatorAdminId.HasValue && vote.InitiatorAdminId.Value != currentAdminId)
            {
                await _notificationService.NotifyAsync(vote.InitiatorAdminId.Value, "Proposal rejected",
                    $"Your {vote.ActionType} proposal for user #{vote.TargetUserId} was rejected by other admins.", ct);
            }

            // Self-applied verification: let the applicant know they can fix and re-apply.
            if (vote.ActionType == "VerifyLawyer" && !vote.InitiatorAdminId.HasValue)
            {
                await _notificationService.NotifyAsync(vote.TargetUserId, "Application not approved",
                    "Your verification application was not approved. You may update your details and re-apply.", ct);
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);

        return new CastVoteResponse
        {
            IsFinalized = finalized,
            CurrentApprovals = vote.ApprovalCount,
            Message = finalized ? "Threshold reached. The requested action has been applied." : "Vote recorded successfully."
        };
    }

    private async Task ExecuteVoteAction(AdminVote vote, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(vote.TargetUserId.ToString());
        if (user == null) return;

        switch (vote.ActionType)
        {
            case "BanUser":
                await _userManager.SetLockoutEnabledAsync(user, true);
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                user.IsDeleted = true;
                await _userManager.UpdateAsync(user);

                await _notificationService.NotifyAsync(user.Id,
                    "Account suspended",
                    "Your account has been banned by platform administrators.", ct);
                break;

            case "VerifyLawyer":
                var lawyerProfile = await _unitOfWork.LawyerProfiles.Query()
                    .FirstOrDefaultAsync(lp => lp.UserId == vote.TargetUserId, ct);
                if (lawyerProfile != null)
                {
                    lawyerProfile.IsVerified = true;

                    if (user.Role == Roles.PendingLawyer)
                    {
                        user.Role = Roles.Lawyer;
                        await _userManager.UpdateAsync(user);
                    }

                    await _unitOfWork.SaveChangesAsync(ct);

                    // ✅ this is the notification that matters most — it's the
                    // whole point of the verification flow being reachable at all
                    await _notificationService.NotifyAsync(user.Id,
                        "You're verified!",
                        "Congratulations — your lawyer account has been verified. You now have full platform access.", ct);
                }
                break;

            case "UnverifyLawyer":
                var unverifyProfile = await _unitOfWork.LawyerProfiles.Query()
                    .FirstOrDefaultAsync(lp => lp.UserId == vote.TargetUserId, ct);
                if (unverifyProfile != null)
                {
                    unverifyProfile.IsVerified = false;

                    if (user.Role == Roles.Lawyer)
                    {
                        user.Role = Roles.PendingLawyer;
                        await _userManager.UpdateAsync(user);
                    }

                    await _unitOfWork.SaveChangesAsync(ct);

                    await _notificationService.NotifyAsync(user.Id,
                        "Verification revoked",
                        "Your lawyer verification has been revoked pending review. Please contact support for details.", ct);
                }
                break;
        }
    }
}