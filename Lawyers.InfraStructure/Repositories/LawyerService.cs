using Lawyers.Application.DTOs;
using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Infrastructure.Services;

public class LawyerService : ILawyerService
{
    private readonly IUnitOfWork _unitOfWork;

    public LawyerService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<LawyerDto>> SearchLawyersAsync(GetLawyersQuery query)
    {
        // 1. Start with an IQueryable. 
        // IMPORTANT: Only show lawyers who are verified by the Admin!
        IQueryable<LawyerProfile> queryable = _unitOfWork.LawyerProfiles
            .Query()
            .Where(l => l.IsVerified);

        // 2. Apply Filters (Only if the user provided them)
        if (!string.IsNullOrWhiteSpace(query.City))
        {
            queryable = queryable.Where(l => l.Address.City.ToLower().Contains(query.City.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(query.Specialization))
        {
            queryable = queryable.Where(l => l.Specialization.ToLower().Contains(query.Specialization.ToLower()));
        }

        if (query.MaxHourlyRate.HasValue)
        {
            queryable = queryable.Where(l => l.HourlyRate <= query.MaxHourlyRate.Value);
        }

        // 3. Apply Sorting
        queryable = query.SortBy?.ToLower() switch
        {
            "rating" => query.IsDescending ? queryable.OrderByDescending(l => l.AverageRating) : queryable.OrderBy(l => l.AverageRating),
            "name" => query.IsDescending ? queryable.OrderByDescending(l => l.FullName) : queryable.OrderBy(l => l.FullName),
            _ => query.IsDescending ? queryable.OrderByDescending(l => l.HourlyRate) : queryable.OrderBy(l => l.HourlyRate)
        };

        // 4. Get Total Count (Executes a SELECT COUNT(*) in SQL)
        var totalCount = await queryable.CountAsync();

        // 5. Apply Pagination & Map to DTO (Executes a highly optimized SELECT in SQL)
        var items = await queryable
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(l => new LawyerDto
            {
                Id = l.Id,
                FullName = l.FullName,
                Bio = l.Bio,
                HourlyRate = l.HourlyRate,
                Specialization = l.Specialization,
                City = l.Address.City, // EF Core is smart enough to join the Address value object!
                AverageRating = l.AverageRating
            })
            .ToListAsync();

        // 6. Return the wrapped result
        return new PagedResult<LawyerDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }
}