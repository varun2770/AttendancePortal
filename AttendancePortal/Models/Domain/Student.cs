using System.ComponentModel.DataAnnotations;

namespace AttendancePortal.Models.Domain;

public class Student
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(30)]
    public string RollNumber { get; set; } = string.Empty;

    public int? CourseId { get; set; }
    public Course? Course { get; set; }

    public bool FaceRegistered { get; set; } = false;

    // The face "fingerprint" (a list of numbers) that OpenCV produces when the student registers.
    public byte[]? FaceEmbedding { get; set; }

    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
}