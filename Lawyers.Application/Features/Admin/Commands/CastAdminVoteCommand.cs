using Lawyers.Application.DTOs;
using MediatR;

namespace Lawyers.Application.Features.Admin.Commands;

public record CastAdminVoteCommand(int VoteId, bool IsApproved) : IRequest<CastVoteResponse>;