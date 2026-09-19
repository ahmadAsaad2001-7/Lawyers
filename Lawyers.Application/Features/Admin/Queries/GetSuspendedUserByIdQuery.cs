using Lawyers.Application.Features.DTOs;
using MediatR;

namespace Lawyers.Application.Features.Admin.Queries;

public record GetSuspendedUserByIdQuery(int UserId) : IRequest<SuspendedUserDto?>;
