using Lawyers.Application.DTOs;
using MediatR;

namespace Lawyers.Application.Features.Lawyers.Queries;

public record GetLawyersQuery : IRequest<PagedResult<LawyerDto>>
{
    public string? City { get; set; }
    public string? Specialization { get; set; }
    public decimal? MaxHourlyRate { get; set; }
    
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    
    public string SortBy { get; set; } = "HourlyRate"; 
    public bool IsDescending { get; set; } = false;
}