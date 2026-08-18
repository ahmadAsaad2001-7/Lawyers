using Lawyers.Application.DTOs;
using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Admin.Commands;

public class CastAdminVoteHandler : IRequestHandler<CastAdminVoteCommand, CastVoteResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly UserManager<User> _userManager;
    
    private const int APPROVAL_THRESHOLD = 2; // Require 2 admins to agree

    public CastAdminVoteHandler(
        IUnitOfWork unitOfWork, 
        ICurrentUserService currentUserService,
        UserManager<User> userManager)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _userManager = userManager;
    }

    public async Task<CastVoteResponse> Handle(CastAdminVoteCommand request, CancellationToken ct)
    {
        var currentAdminId = _currentUserService.UserId!.Value;

        // 1. Find the vote
        var vote = await _unitOfWork.AdminVotes.Query()
            .Include(v => v.TargetUser)
            .FirstOrDefaultAsync(v => v.Id == request.VoteId, ct);

        if (vote == null || vote.IsResolved)
            throw new Exception("Vote not found or already finalized.");

        // 2. Check for duplicate voting
        var alreadyVoted = await _unitOfWork.VoteParticipants.Query()
            .AnyAsync(p => p.AdminVoteId == request.VoteId && p.AdminUserId == currentAdminId, ct);

        if (alreadyVoted)
            throw new Exception("You have already voted on this decision.");

        // 3. Record the vote
        if (request.IsApproved) vote.ApprovalCount++;
        else vote.DisapprovalCount++;

        var participant = new VoteParticipant
        {
            AdminVoteId = request.VoteId,
            AdminUserId = currentAdminId,
            IsApproved = request.IsApproved
        };
        await _unitOfWork.VoteParticipants.AddAsync(participant, ct);

        // 4. Check if threshold is met
        bool finalized = false;
        if (vote.ApprovalCount >= APPROVAL_THRESHOLD)
        {
            finalized = true;
            vote.IsResolved = true;
            await ExecuteVoteAction(vote);
        }
        // Optional: Add a rejection threshold (e.g., if DisapprovalCount >= 2, finalize as rejected)

        await _unitOfWork.SaveChangesAsync(ct);

        return new CastVoteResponse
        {
            IsFinalized = finalized,
            CurrentApprovals = vote.ApprovalCount,
            Message = finalized ? "Threshold reached. User has been banned." : "Vote recorded successfully."
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // THE EXECUTION ENGINE
    // ═══════════════════════════════════════════════════════════════
    private async Task ExecuteVoteAction(AdminVote vote)
    {
        if (vote.ActionType == "BanUser")
        {
            var user = await _userManager.FindByIdAsync(vote.TargetUserId.ToString());
            if (user != null)
            {
                // 1. Lock the user out indefinitely (Standard Identity practice)
                await _userManager.SetLockoutEnabledAsync(user, true);
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

                // 2. Soft delete in your custom domain (Optional but recommended)
                user.IsDeleted = true;
                await _userManager.UpdateAsync(user);
            }
        }
        
        // Future extensibility:
        // else if (vote.ActionType == "VerifyLawyer") { ... }
        // else if (vote.ActionType == "RefundPayment") { ... }
    }
}