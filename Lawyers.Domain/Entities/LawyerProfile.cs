    using Lawyers.Domain.ValueObjects;

    namespace Lawyers.Domain.Entities;

    public class LawyerProfile : AuditableEntity
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        
        public string FullName { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public decimal HourlyRate { get; set; }
        
        public Address Address { get; set; } = null!;
        public string BarLicenseNumber { get; set; } = string.Empty;
        public bool IsVerified { get; set; } = false;
        public string Specialization { get; set; } = string.Empty; // e.g., "Family Law", "Corporate"
        public decimal AverageRating { get; set; } = 0.0m;
        public string LawFirmName { get; set; } = string.Empty;
        public ICollection<LawyerPost> Posts { get; set; } = new List<LawyerPost>();
        public ICollection<Consultation> Consultations { get; set; } = new List<Consultation>();
        public ICollection<LawyerWeeklySchedule> WeeklySchedules { get; set; } = new List<LawyerWeeklySchedule>();
        public ICollection<LawyerAvailabilityException> AvailabilityExceptions { get; set; } = new List<LawyerAvailabilityException>();

    public ICollection<FreeConsultationMessage> FreeMessages { get; set; } = new List<FreeConsultationMessage>();
    }