using Lawyers.Application.DTOs;
using Lawyers.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Admin.Queries;
public record GetUserByIdQuery(int UserId) : IRequest<UserListDto?>;

public class GetUserByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetUserByIdQuery, UserListDto?>
{
    public async Task<UserListDto?> Handle(GetUserByIdQuery q, CancellationToken ct)
    {
        return await unitOfWork.Users.Query().AsNoTracking()
            .Include(u => u.LawyerProfile).Include(u => u.ClientProfile)
            .Where(u => u.Id == q.UserId)
            .Select(u => new UserListDto
            {
                Id = u.Id, Email = u.Email!, PhoneNumber = u.PhoneNumber,
                Role = u.Role.ToString(), ProfileImageUrl = u.ProfileImageUrl,
                CreatedAt = u.CreatedAt, IsDeleted = u.IsDeleted,
                DisplayName = u.LawyerProfile != null ? u.LawyerProfile.FullName
                    : u.ClientProfile != null ? u.ClientProfile.FullName : u.Email!,
                LawyerProfileId = u.LawyerProfile != null ? u.LawyerProfile.Id : null,
                Specialization = u.LawyerProfile != null ? u.LawyerProfile.Specialization : null,
                HourlyRate = u.LawyerProfile != null ? u.LawyerProfile.HourlyRate : null,
                IsVerified = u.LawyerProfile != null ? u.LawyerProfile.IsVerified : null,
                AverageRating = u.LawyerProfile != null ? u.LawyerProfile.AverageRating : null,
                LawFirmName = u.LawyerProfile != null ? u.LawyerProfile.LawFirmName : null,
                ClientProfileId = u.ClientProfile != null ? u.ClientProfile.Id : null,
                ConsultationCount =
                    (u.ClientProfile != null ? u.ClientProfile.Consultations.Count : 0) +
                    (u.LawyerProfile != null ? u.LawyerProfile.Consultations.Count : 0),
                PostsCount = u.LawyerProfile != null ? u.LawyerProfile.Posts.Count : 0,
                FreeMessagesCount = u.LawyerProfile != null ? u.LawyerProfile.FreeMessages.Count : 0,
            })
            .FirstOrDefaultAsync(ct);
    }
}