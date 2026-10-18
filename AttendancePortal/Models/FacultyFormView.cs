using System.ComponentModel.DataAnnotations;

namespace AttendancePortal.Models;

public class FacultyFormViewModel
{
    public int Id { get; set; }

    [Required, EmailAddress]
    [Display(Name = "Email (used to log in)")]
    public string Email { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string? Password { get; set; }

    [Required, StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Department { get; set; } = string.Empty;
}