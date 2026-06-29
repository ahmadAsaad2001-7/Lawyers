using Lawyers.Application.DTOs;
using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Lawyers.Queries;

public class GetLawyersQueryHandler : IRequestHandler<GetLawyersQuery, PagedResult<LawyerDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetLawyersQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<LawyerDto>> Handle(GetLawyersQuery request, CancellationToken cancellationToken)
    {
        // 1. Base Query (Only verified lawyers)
        IQueryable<LawyerProfile> queryable = _unitOfWork.LawyerProfiles
            .Query()
            .Where(l => l.IsVerified);

        // 2. Apply Filters
        if (!string.IsNullOrWhiteSpace(request.City))
            queryable = queryable.Where(l => l.Address.City.ToLower().Contains(request.City.ToLower()));

        if (!string.IsNullOrWhiteSpace(request.Specialization))
            queryable = queryable.Where(l => l.Specialization.ToLower().Contains(request.Specialization.ToLower()));

        if (request.MaxHourlyRate.HasValue)
            queryable = queryable.Where(l => l.HourlyRate <= request.MaxHourlyRate.Value);

        // 3. Apply Sorting
        queryable = request.SortBy?.ToLower() switch
        {
            "rating" => request.IsDescending ? queryable.OrderByDescending(l => l.AverageRating) : queryable.OrderBy(l => l.AverageRating),
            "name" => request.IsDescending ? queryable.OrderByDescending(l => l.FullName) : queryable.OrderBy(l => l.FullName),
            _ => request.IsDescending ? queryable.OrderByDescending(l => l.HourlyRate) : queryable.OrderBy(l => l.HourlyRate)
        };

        // 4. Get Total Count
        var totalCount = await queryable.CountAsync(cancellationToken);

        // 5. Apply Pagination & Map to DTO
        var items = await queryable
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(l => new LawyerDto
            {
                Id = l.Id,
                FullName = l.FullName,
                Bio = l.Bio,
                HourlyRate = l.HourlyRate,
                Specialization = l.Specialization,
                City = l.Address.City,
                AverageRating = l.AverageRating
            })
            .ToListAsync(cancellationToken);

        // 6. Return Paged Result
        return new PagedResult<LawyerDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}