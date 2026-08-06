using Lawyers.Application.DTOs;
using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // <-- Make sure to add this namespace

namespace Lawyers.Application.Features.Lawyers.Queries;

public class GetLawyersQueryHandler : IRequestHandler<GetLawyersQuery, PagedResult<LawyerDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetLawyersQueryHandler> _logger; // <-- Inject Logger

    public GetLawyersQueryHandler(IUnitOfWork unitOfWork, ILogger<GetLawyersQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PagedResult<LawyerDto>> Handle(GetLawyersQuery request, CancellationToken cancellationToken)
    {
        // 1. Log the incoming request data before doing anything
        _logger.LogInformation("[GetLawyersQuery] Incoming request received. City: {City},State: {State}, Specialization: {Specialization}, MaxHourlyRate: {MaxHourlyRate}, PageNumber: {PageNumber}, PageSize: {PageSize}, SortBy: {SortBy}, IsDescending: {IsDescending}", 
            request.City,request.State, request.Specialization, request.MaxHourlyRate, request.PageNumber, request.PageSize, request.SortBy, request.IsDescending);

        try
        {
            // Base Query (Only verified lawyers)
            _logger.LogDebug("[GetLawyersQuery] Building IQueryable expression tree.");
            IQueryable<LawyerProfile> queryable = _unitOfWork.LawyerProfiles
                .Query()
                .Where(l => l.IsVerified);

            // 2. Apply Filters
            if (!string.IsNullOrWhiteSpace(request.City))
            {
                _logger.LogDebug("[GetLawyersQuery] Applying City filter: {City}", request.City);
                queryable = queryable.Where(l => l.Address.City.ToLower().Contains(request.City.ToLower()));
            }

            if (!string.IsNullOrEmpty(request.State))
            {
                _logger.LogDebug("[GetLawyersQuery] Applying State Filter:{State}", request.State);
                queryable=queryable.Where(l=>l.Address.State.ToLower().Contains(request.State.ToLower()));
            }
            if (!string.IsNullOrWhiteSpace(request.Specialization))
            {
                _logger.LogDebug("[GetLawyersQuery] Applying Specialization filter: {Specialization}", request.Specialization);
                queryable = queryable.Where(l => l.Specialization.ToLower().Contains(request.Specialization.ToLower()));
            }

            if (request.MaxHourlyRate.HasValue)
            {
                _logger.LogDebug("[GetLawyersQuery] Applying MaxHourlyRate filter: {MaxHourlyRate}", request.MaxHourlyRate.Value);
                queryable = queryable.Where(l => l.HourlyRate <= request.MaxHourlyRate.Value);
            }

            // 3. Apply Sorting
            _logger.LogDebug("[GetLawyersQuery] Applying sorting by: {SortBy} (Descending: {IsDescending})", request.SortBy, request.IsDescending);
            queryable = request.SortBy?.ToLower() switch
            {
                "rating" => request.IsDescending ? queryable.OrderByDescending(l => l.AverageRating) : queryable.OrderBy(l => l.AverageRating),
                "name" => request.IsDescending ? queryable.OrderByDescending(l => l.FullName) : queryable.OrderBy(l => l.FullName),
                _ => request.IsDescending ? queryable.OrderByDescending(l => l.HourlyRate) : queryable.OrderBy(l => l.HourlyRate)
            };

            // 4. Get Total Count (First DB Execution Point)
            _logger.LogInformation("[GetLawyersQuery] Executing database CountAsync query...");
            var totalCount = await queryable.CountAsync(cancellationToken);
            _logger.LogInformation("[GetLawyersQuery] Database CountAsync executed successfully. Total Count: {TotalCount}", totalCount);

            // 5. Apply Pagination & Map to DTO (Second DB Execution Point)
            _logger.LogInformation("[GetLawyersQuery] Executing database ToListAsync query with Skip: {Skip}, Take: {Take}...", (request.PageNumber - 1) * request.PageSize, request.PageSize);
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

            _logger.LogInformation("[GetLawyersQuery] Database ToListAsync executed successfully. Retreived {Count} items.", items.Count);

            // 6. Formulate response object
            var result = new PagedResult<LawyerDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };

            // Log final successful completion details
            _logger.LogInformation("[GetLawyersQuery] Request processed successfully. Returning {Count} elements for page {PageNumber}.", result.Items.Count, result.PageNumber);
            return result;
        }
        catch (Exception ex)
        {
            // 7. Catch any system or database runtime crashes instantly
            _logger.LogError(ex, "[GetLawyersQuery] An error occurred while executing the query handler logic. Request Parameters: {@Request}", request);
            throw; // Re-throw the exception so the API Controller handles the application response lifecycle correctly
        }
    }
}