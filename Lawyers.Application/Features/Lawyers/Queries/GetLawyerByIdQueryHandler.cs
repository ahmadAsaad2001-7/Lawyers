using Lawyers.Application.DTOs;
using Lawyers.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Lawyers.Queries;

public class GetLawyerByIdQueryHandler : IRequestHandler<GetLawyerByIdQuery, LawyerDto>
{
    private readonly IUnitOfWork _unitOfWork;
    
    public GetLawyerByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<LawyerDto> Handle(GetLawyerByIdQuery request, CancellationToken cancellationToken)
    {
        var lawyer = await _unitOfWork.LawyerProfiles.Query()
            .Include(l => l.User) // ✅ CRITICAL: Include User to access ProfileImageUrl and PhoneNumber
            .Where(l => l.Id == request.Id)
            .Select(l => new LawyerDto
            {
                Id = l.Id,
                FullName = l.FullName,
                Avatar = l.User != null ? l.User.ProfileImageUrl : null,
                Bio = l.Bio,
                HourlyRate = l.HourlyRate,
                Specialization = l.Specialization,
                City = l.Address.City,
                State = l.Address.State,
                AverageRating = l.AverageRating,
                IsVerified = l.IsVerified,
                LawFirmName = l.LawFirmName,     
                Phone = l.User.PhoneNumber       
            })
            .FirstOrDefaultAsync(cancellationToken);
            
        return lawyer;
    }
}