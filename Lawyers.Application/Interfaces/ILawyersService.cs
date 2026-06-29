using Lawyers.Application.DTOs;

namespace Lawyers.Application.Interfaces;

public interface ILawyerService
{
    Task<PagedResult<LawyerDto>> SearchLawyersAsync(GetLawyersQuery query);
}