using Lawyers.Application.DTOs;
using Lawyers.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Lawyers.Queries;

public class GetLawyerByIdQueryHandler : IRequestHandler<GetLawyerByIdQuery,LawyerDto>
{
    private readonly IUnitOfWork _unitOfWork;
    public GetLawyerByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public async Task<LawyerDto> Handle(GetLawyerByIdQuery request, CancellationToken cancellationToken)
    {
        var lawyer =await _unitOfWork.LawyerProfiles.Query().Where(l=>l.Id==request.Id).Select(
            l=>new LawyerDto
            {
                Id=l.Id,
                FullName=l.FullName,
                Bio=l.Bio,
                HourlyRate = l.HourlyRate,
                Specialization =  l.Specialization,
                City = l.Address.City,
                AverageRating =  l.AverageRating,
                IsVerified = l.IsVerified
                
                
            }
            ).FirstOrDefaultAsync(cancellationToken);
        return lawyer;
    }
}