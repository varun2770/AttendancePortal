namespace AttendancePortal.Models.Domain;

public enum AttendanceStatus
{
    Present,
    Absent
}

public class AttendanceRecord
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public Student? Student { get; set; }

    public int CourseId { get; set; }
    public Course? Course { get; set; }

    public DateOnly Date { get; set; }

    public AttendanceStatus Status { get; set; } = AttendanceStatus.Absent;

    public DateTime? MarkedAt { get; set; }

    public string MarkedBy { get; set; } = "Manual";
}