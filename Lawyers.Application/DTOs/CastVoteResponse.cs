namespace Lawyers.Application.DTOs;

public class CastVoteResponse
{
    
    public bool IsFinalized { get; set; }
    public int CurrentApprovals { get; set; }
    public string Message { get; set; } = string.Empty;
}