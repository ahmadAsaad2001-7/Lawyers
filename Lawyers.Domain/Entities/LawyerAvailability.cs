namespace Lawyers.Domain.Entities;

public class LawyerAvailability : BaseEntity
{
    public int LawyerProfileId { get; set; }
    public LawyerProfile LawyerProfile { get; set; } = null!;
    public DateTime Date { get; set; }  
    public int Hour { get; set; }
}