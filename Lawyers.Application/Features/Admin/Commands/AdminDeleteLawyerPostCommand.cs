
using Lawyers.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Admin.Commands;

public record AdminDeleteLawyerPostCommand(int PostId, string Reason) : IRequest<bool>;

public class AdminDeleteLawyerPostCommandHandler(IUnitOfWork unitOfWork) 
    : IRequestHandler<AdminDeleteLawyerPostCommand, bool>
{
    public async Task<bool> Handle(AdminDeleteLawyerPostCommand request, CancellationToken cancellationToken)
    {
        // 1. Find the post (NO ownership check - admin can delete ANY post)
        var post = await unitOfWork.LawyerPosts.Query()
            .Include(p => p.Lawyer).ThenInclude(l => l.User) // Include lawyer to notify them later if needed
            .FirstOrDefaultAsync(p => p.Id == request.PostId, cancellationToken);

        if (post == null) return false;

        // 2. Delete the post 
        // (Matches your Lawyer controller's hard-delete pattern)
        unitOfWork.LawyerPosts.Delete(post);
        
        // 💡 Optional: If you have an AuditLog or Notification table, 
        // you would save the `request.Reason` here so the lawyer knows why it was removed.

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
