using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AttendancePortal.Models;

public class CourseFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(20)]
    [Display(Name = "Course code")]
    public string CourseCode { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Course name")]
    public string CourseName { get; set; } = string.Empty;

    [Display(Name = "Faculty")]
    public int? FacultyId { get; set; }

    public List<SelectListItem> FacultyOptions { get; set; } = new();
}