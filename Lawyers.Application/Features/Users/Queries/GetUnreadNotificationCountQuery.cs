using MediatR;

namespace Lawyers.Application.Features.Users.Queries;

public record GetUnreadNotificationCountQuery() : IRequest<int>;
