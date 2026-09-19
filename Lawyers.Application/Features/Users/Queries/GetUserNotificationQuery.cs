using Lawyers.Application.DTOs;
using MediatR;

namespace Lawyers.Application.Features.Users.Queries;

public record GetNotificationsQuery(bool UnreadOnly = false, int Limit = 50) : IRequest<List<NotificationDto>>;
