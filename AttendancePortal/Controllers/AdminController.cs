using AttendancePortal.Data;
using AttendancePortal.Models;
using AttendancePortal.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AttendancePortal.Controllers;

[Authorize(Roles = Roles.Admin)]
public partial class AdminController : Controller
{
    private readonly ApplicationDbContext _db;

    public AdminController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.StudentCount = await _db.Students.CountAsync();
        ViewBag.FacultyCount = await _db.Faculties.CountAsync();
        ViewBag.CourseCount = await _db.Courses.CountAsync();
        return View();
    }

    // ---------- Courses ----------

    public async Task<IActionResult> Courses()
    {
        var courses = await _db.Courses
            .Include(c => c.Faculty)
            .OrderBy(c => c.CourseCode)
            .ToListAsync();

        var counts = new Dictionary<int, int>();
        foreach (var c in courses)
        {
            counts[c.Id] = await _db.Students.CountAsync(s => s.CourseId == c.Id);
        }

        ViewBag.Counts = counts;
        return View(courses);
    }

    [HttpGet]
    public async Task<IActionResult> CreateCourse()
    {
        var model = new CourseFormViewModel { FacultyOptions = await GetFacultyOptionsAsync() };
        return View("CourseForm", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCourse(CourseFormViewModel model)
    {
        model.CourseCode = model.CourseCode?.Trim().ToUpper() ?? "";

        if (await _db.Courses.AnyAsync(c => c.CourseCode == model.CourseCode))
        {
            ModelState.AddModelError(nameof(model.CourseCode), "A course with this code already exists.");
        }

        if (!ModelState.IsValid)
        {
            model.FacultyOptions = await GetFacultyOptionsAsync();
            return View("CourseForm", model);
        }

        _db.Courses.Add(new Course
        {
            CourseCode = model.CourseCode,
            CourseName = model.CourseName.Trim(),
            FacultyId = model.FacultyId
        });
        await _db.SaveChangesAsync();

        TempData["Message"] = "Course created.";
        return RedirectToAction(nameof(Courses));
    }

    [HttpGet]
    public async Task<IActionResult> EditCourse(int id)
    {
        var course = await _db.Courses.FindAsync(id);
        if (course is null) return NotFound();

        var model = new CourseFormViewModel
        {
            Id = course.Id,
            CourseCode = course.CourseCode,
            CourseName = course.CourseName,
            FacultyId = course.FacultyId,
            FacultyOptions = await GetFacultyOptionsAsync()
        };
        return View("CourseForm", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCourse(CourseFormViewModel model)
    {
        var course = await _db.Courses.FindAsync(model.Id);
        if (course is null) return NotFound();

        model.CourseCode = model.CourseCode?.Trim().ToUpper() ?? "";

        if (await _db.Courses.AnyAsync(c => c.CourseCode == model.CourseCode && c.Id != model.Id))
        {
            ModelState.AddModelError(nameof(model.CourseCode), "Another course already uses this code.");
        }

        if (!ModelState.IsValid)
        {
            model.FacultyOptions = await GetFacultyOptionsAsync();
            return View("CourseForm", model);
        }

        course.CourseCode = model.CourseCode;
        course.CourseName = model.CourseName.Trim();
        course.FacultyId = model.FacultyId;
        await _db.SaveChangesAsync();

        TempData["Message"] = "Course updated.";
        return RedirectToAction(nameof(Courses));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCourse(int id)
    {
        var course = await _db.Courses.FindAsync(id);
        if (course is null) return NotFound();

        var hasStudents = await _db.Students.AnyAsync(s => s.CourseId == id);
        var hasAttendance = await _db.AttendanceRecords.AnyAsync(a => a.CourseId == id);

        if (hasStudents || hasAttendance)
        {
            TempData["Error"] = "This course has enrolled students or attendance records, so it can't be deleted.";
            return RedirectToAction(nameof(Courses));
        }

        _db.Courses.Remove(course);
        await _db.SaveChangesAsync();

        TempData["Message"] = "Course deleted.";
        return RedirectToAction(nameof(Courses));
    }

    private async Task<List<SelectListItem>> GetFacultyOptionsAsync()
    {
        var faculties = await _db.Faculties.OrderBy(f => f.FullName).ToListAsync();
        return faculties
            .Select(f => new SelectListItem($"{f.FullName} ({f.Department})", f.Id.ToString()))
            .ToList();
    }
}