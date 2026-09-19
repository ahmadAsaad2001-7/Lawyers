

// Lawyers.Application/Features/Auth/Commands/RefreshTokenCommand.cs
using Lawyers.Application.DTOs.Auth;
using MediatR;

namespace Lawyers.Application.Features.Auth.Commands;
public record RefreshTokenCommand : IRequest<AuthResponseDto>;