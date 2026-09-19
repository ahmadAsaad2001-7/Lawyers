// Lawyers.Application/Features/Lawyers/Commands/CompleteLawyerProfileHandler.cs
using Lawyers.Application.Interfaces;
using Lawyers.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace Lawyers.Application.Features.Lawyers.Commands;

public class CompleteLawyerProfileHandler : IRequestHandler<CompleteLawyerProfileCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public CompleteLawyerProfileHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(CompleteLawyerProfileCommand request, CancellationToken ct)
    {
        var userId = _currentUserService.UserId!.Value;

        var lawyerProfile = await _unitOfWork.LawyerProfiles.Query()
            .FirstOrDefaultAsync(lp => lp.UserId == userId, ct);

        if (lawyerProfile == null)
            throw new InvalidOperationException("No lawyer profile found for this account.");

        if (lawyerProfile.IsVerified)
            throw new InvalidOperationException("Your profile is already verified and cannot be edited here.");

        if (string.IsNullOrWhiteSpace(request.BarLicenseNumber))
            throw new InvalidOperationException("Bar license number is required.");

        lawyerProfile.BarLicenseNumber = request.BarLicenseNumber.Trim();

        if (!string.IsNullOrWhiteSpace(request.Specialization))
            lawyerProfile.Specialization = request.Specialization;

        if (!string.IsNullOrWhiteSpace(request.Bio))
            lawyerProfile.Bio = request.Bio;

        if (request.HourlyRate.HasValue)
            lawyerProfile.HourlyRate = request.HourlyRate.Value;

        lawyerProfile.Address = new Address
        {
            Street = request.Street ?? lawyerProfile.Address.Street,
            City = request.City ?? lawyerProfile.Address.City,
            State = request.State ?? lawyerProfile.Address.State,
            Country = request.Country ?? lawyerProfile.Address.Country,
            PostalCode = request.PostalCode ?? lawyerProfile.Address.PostalCode
        };

        _unitOfWork.LawyerProfiles.Update(lawyerProfile);
        await _unitOfWork.SaveChangesAsync(ct);

        return true;
    }
}