using Lawyers.Application.DTOs;
using MediatR;

namespace Lawyers.Application.Features.Lawyers.Queries;

public record GetLawyerByIdQuery(int Id ):IRequest<LawyerDto>;