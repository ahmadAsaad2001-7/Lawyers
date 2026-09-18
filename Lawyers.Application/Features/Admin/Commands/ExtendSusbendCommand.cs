using MediatR;

namespace Lawyers.Application.Features.Admin.Commands;

public record ExtendSuspendCommand(int UserId, int AdditionalDays) : IRequest<bool>;
