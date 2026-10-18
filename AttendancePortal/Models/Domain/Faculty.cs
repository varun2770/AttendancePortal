using System.ComponentModel.DataAnnotations;

namespace AttendancePortal.Models.Domain;

public class Faculty
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(120)]
    public string? Department { get; set; }

    public ICollection<Course> Courses { get; set; } = new List<Course>();
}
