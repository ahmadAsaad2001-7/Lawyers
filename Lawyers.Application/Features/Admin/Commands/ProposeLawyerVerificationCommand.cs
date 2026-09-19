using MediatR;

namespace Lawyers.Application.Features.Admin.Commands;

public record ProposeLawyerVerificationCommand(int LawyerUserId, string Reason) : IRequest<int>;