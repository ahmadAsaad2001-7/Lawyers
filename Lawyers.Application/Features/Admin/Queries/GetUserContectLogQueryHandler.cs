using Lawyers.Application.DTOs;
using Lawyers.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Admin.Queries;

public class GetUserContactLogQueryHandler : IRequestHandler<GetUserContactLogQuery, PagedResult<UserContactLogDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserContactLogQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<UserContactLogDto>> Handle(GetUserContactLogQuery request, CancellationToken ct)
    {
        // ── STEP 1: Get the target user to know their Email, ClientProfile.Id, and LawyerProfile.Id ──
        var user = await _unitOfWork.Users.Query()
            .Include(u => u.ClientProfile)
            .Include(u => u.LawyerProfile)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, ct);

        if (user == null) 
            return new PagedResult<UserContactLogDto> { Items = new List<UserContactLogDto>(), TotalCount = 0 };

        var contacts = new List<UserContactLogDto>();

        // ── STEP 2: Fetch Consultations (Registered User to Registered User) ──
        var consultations = await _unitOfWork.Consultations.Query()
            .Include(c => c.Client).ThenInclude(c => c.User)
            .Include(c => c.Lawyer).ThenInclude(l => l.User)
            .Where(c => c.Client.UserId == request.UserId || c.Lawyer.UserId == request.UserId)
            .ToListAsync(ct);

        foreach (var c in consultations)
        {
            contacts.Add(new UserContactLogDto
            {
                ContactType = "Consultation",
                RelatedEntityId = c.Id,
                Timestamp = c.CreatedAt,
                InitiatorName = c.Client.FullName,
                InitiatorRole = "Client",
                TargetName = c.Lawyer.FullName,
                TargetRole = "Lawyer",
                Status = c.Status.ToString()
            });
        }

        // ── STEP 3: Fetch Free Inquiries (Anonymous/Public to Registered Lawyer) ──
        // Fixed: Use `FreeMessages` and match by Email or LawyerId
        var inquiries = await _unitOfWork.FreeMessages.Query() 
            .Include(f => f.Lawyer).ThenInclude(l => l.User)
            .Where(f => f.SenderEmail == user.Email || f.Lawyer.UserId == request.UserId)
            .ToListAsync(ct);

        foreach (var f in inquiries)
        {
            contacts.Add(new UserContactLogDto
            {
                ContactType = "Free Inquiry",
                RelatedEntityId = f.Id,
                Timestamp = f.CreatedAt,
                InitiatorName = f.SenderName, // From the FreeConsultationMessage table
                InitiatorRole = "Public User",
                TargetName = f.Lawyer.FullName,
                TargetRole = "Lawyer",
                Status = f.IsRepliedTo ? "Replied" : "Pending"
            });
        }

        // ── STEP 4: Merge, Sort, and Paginate ──
        var sortedContacts = contacts
            .OrderByDescending(c => c.Timestamp)
            .ToList();

        var totalCount = sortedContacts.Count;
        var pagedItems = sortedContacts
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new PagedResult<UserContactLogDto>
        {
            Items = pagedItems,
            TotalCount = totalCount,
            PageNumber = request.Page,
            PageSize = request.PageSize
        };
    }
}