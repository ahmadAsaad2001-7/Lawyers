using MediatR;

namespace Lawyers.Application.Features.Admin.Commands;

public record ProposeLawyerUnverificationCommand(int LawyerUserId, string Reason) : IRequest<int>;