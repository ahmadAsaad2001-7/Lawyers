using Lawyers.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Lawyers.Posts.Commands;

// ✅ LawyerId removed
public record DeleteLawyerPostCommand(int Id) : IRequest<bool>;

public class DeleteLawyerPostCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    : IRequestHandler<DeleteLawyerPostCommand, bool>
{
    public async Task<bool> Handle(DeleteLawyerPostCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;

        var lawyerProfile = await unitOfWork.LawyerProfiles.Query()
            .FirstOrDefaultAsync(lp => lp.UserId == userId, cancellationToken);

        if (lawyerProfile == null)
            throw new UnauthorizedAccessException("Only lawyers can delete posts.");

        var post = await unitOfWork.LawyerPosts.Query()
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.LawyerId == lawyerProfile.Id, cancellationToken);

        if (post == null) return false;

        unitOfWork.LawyerPosts.Delete(post);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}