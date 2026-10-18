using AttendancePortal.Models.Domain;

namespace AttendancePortal.Models;

public class MarkAttendanceViewModel
{
    public Course Course { get; set; } = null!;
    public int EnrolledCount { get; set; }
    public int RegisteredCount { get; set; }
    public List<AttendanceRow> Today { get; set; } = new();
}

public record AttendanceRow(string FullName, string RollNumber, string Time);