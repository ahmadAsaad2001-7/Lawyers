// Lawyers.Application/Features/Lawyer/Commands/ApplyForVerificationHandler.cs

using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities;
using Lawyers.Domain.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace Lawyers.Application.Features.Lawyers.Commands; 
public class ApplyForVerificationHandler : IRequestHandler<ApplyForVerificationCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public ApplyForVerificationHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<int> Handle(ApplyForVerificationCommand request, CancellationToken ct)
    {
        var userId = _currentUserService.UserId!.Value;

        // ✅ Only PendingLawyer accounts can apply — already-verified Lawyers
        // and Clients have no business hitting this endpoint.
        if (_currentUserService.Role != Roles.PendingLawyer)
            throw new UnauthorizedAccessException("Only accounts pending lawyer verification can apply.");

        var lawyerProfile = await _unitOfWork.LawyerProfiles.Query()
            .FirstOrDefaultAsync(lp => lp.UserId == userId, ct);

        if (lawyerProfile == null)
            throw new InvalidOperationException("No lawyer profile found for this account.");

        if (lawyerProfile.IsVerified)
            throw new InvalidOperationException("This account is already verified.");

        // ✅ Basic completeness gate — don't let someone apply with placeholder data.
        if (string.IsNullOrWhiteSpace(lawyerProfile.BarLicenseNumber) ||
            lawyerProfile.BarLicenseNumber == "PENDING")
        {
            throw new InvalidOperationException("Please complete your bar license details before applying for verification.");
        }

        var hasActiveVote = await _unitOfWork.AdminVotes.Query()
            .AnyAsync(vote => vote.TargetUserId == userId &&
                              vote.ActionType == "VerifyLawyer" &&
                              !vote.IsResolved, ct);

        if (hasActiveVote)
            throw new InvalidOperationException("You already have a pending verification application.");

        var vote = new AdminVote
        {
            ActionType = "VerifyLawyer",
            TargetUserId = userId,
            InitiatorAdminId = null,          
            AppliedByUserId = userId,        
            Reason = request.Reason ?? "Self-submitted application for lawyer verification.",
            ApprovalCount = 0,             
            IsResolved = false
        };

        await _unitOfWork.AdminVotes.AddAsync(vote, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return vote.Id;
    }
}