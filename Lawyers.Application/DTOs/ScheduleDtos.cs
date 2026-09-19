namespace Lawyers.Application.Features.LawyerSchedule.DTOs;

public class WeeklyScheduleDto
{
    public int Id { get; set; }
    public int Day { get; set; } // 0 = Sunday, 6 = Saturday (matches C# DayOfWeek)
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public bool IsEnabled { get; set; }
}

public class WeeklyScheduleInput
{
    public int Day { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public bool IsEnabled { get; set; }
}

public class ExceptionDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public int Type { get; set; } // ExceptionType enum stored as int
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public string? Reason { get; set; }
}

public class DayPreviewDto
{
    public string Date { get; set; } = string.Empty;
    public string DayName { get; set; } = string.Empty;
    public List<int> AvailableHours { get; set; } = new();
    public bool IsFullyAvailable { get; set; }
    public bool IsFullyClosed { get; set; }
}