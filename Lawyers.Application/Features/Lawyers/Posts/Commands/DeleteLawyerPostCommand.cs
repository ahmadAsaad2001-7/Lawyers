using Lawyers.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lawyers.Application.Features.Lawyers.Topics.Commands;

public record DeleteLawyerPostCommand(int Id, int LawyerId) : IRequest<bool>;

public class DeleteLawyerPostCommandHandler(IUnitOfWork unitOfWork) 
    : IRequestHandler<DeleteLawyerPostCommand, bool>
{
    public async Task<bool> Handle(DeleteLawyerPostCommand request, CancellationToken cancellationToken)
    {
        var post =  await unitOfWork.LawyerPosts.Query()
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.LawyerId == request.LawyerId, cancellationToken);

        if (post == null) return false;

        unitOfWork.LawyerPosts.Delete(post);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}