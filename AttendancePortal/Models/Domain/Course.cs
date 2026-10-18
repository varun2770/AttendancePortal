using System.ComponentModel.DataAnnotations;

namespace AttendancePortal.Models.Domain;

public class Course
{
    public int Id { get; set; }

    [Required, StringLength(20)]
    public string CourseCode { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string CourseName { get; set; } = string.Empty;

    public int? FacultyId { get; set; }
    public Faculty? Faculty { get; set; }

    public ICollection<Student> Students { get; set; } = new List<Student>();
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
}
