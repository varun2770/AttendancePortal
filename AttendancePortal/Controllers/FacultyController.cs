using AttendancePortal.Data;
using AttendancePortal.Models;
using AttendancePortal.Models.Domain;
using AttendancePortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendancePortal.Controllers;

[Authorize(Roles = Roles.Faculty)]
public class FacultyController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public FacultyController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        var faculty = await _db.Faculties
            .Include(f => f.Courses)
            .FirstOrDefaultAsync(f => f.UserId == userId);

        return View(faculty);
    }

    [HttpGet]
    public async Task<IActionResult> MarkAttendance(int courseId)
    {
        var course = await GetOwnCourseAsync(courseId);
        if (course is null)
        {
            return NotFound();
        }

        var today = DateOnly.FromDateTime(DateTime.Now);

        var records = await _db.AttendanceRecords
            .Where(a => a.CourseId == course.Id && a.Date == today && a.Status == AttendanceStatus.Present)
            .Include(a => a.Student)
            .OrderBy(a => a.MarkedAt)
            .ToListAsync();

        var model = new MarkAttendanceViewModel
        {
            Course = course,
            EnrolledCount = await _db.Students.CountAsync(s => s.CourseId == course.Id),
            RegisteredCount = await _db.Students.CountAsync(s => s.CourseId == course.Id && s.FaceRegistered),
            Today = records
                .Select(a => new AttendanceRow(
                    a.Student!.FullName,
                    a.Student.RollNumber,
                    a.MarkedAt?.ToString("HH:mm:ss") ?? ""))
                .ToList()
        };

        return View(model);
    }

    // Called by the camera page: reads one photo, finds who it is, marks them present for today.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAttendance([FromBody] MarkAttendanceRequest request, [FromServices] FaceService faceService)
    {
        var course = await GetOwnCourseAsync(request.CourseId);
        if (course is null)
        {
            return NotFound(new { message = "Course not found." });
        }

        var imageBytes = DecodeImage(request.Image);
        if (imageBytes is null)
        {
            return BadRequest(new { message = "The image data was not valid." });
        }

        var (embedding, error) = faceService.GetEmbedding(imageBytes);
        if (embedding is null)
        {
            return BadRequest(new { message = error });
        }

        // Only students enrolled in this course who have registered a face can be matched.
        var enrolled = await _db.Students
            .Where(s => s.CourseId == course.Id && s.FaceEmbedding != null)
            .ToListAsync();

        if (enrolled.Count == 0)
        {
            return Ok(new { matched = false, message = "No student in this course has registered a face yet." });
        }

        var stored = enrolled.Select(s => (StudentId: s.Id, Embedding: FaceService.FromBytes(s.FaceEmbedding!)));
        var (studentId, score) = FaceService.FindBestMatch(embedding, stored);

        if (studentId is null)
        {
            return Ok(new
            {
                matched = false,
                message = "No matching student found (best similarity " + Math.Round(score, 2) + ")."
            });
        }

        var student = enrolled.First(s => s.Id == studentId);
        var today = DateOnly.FromDateTime(DateTime.Now);

        var record = await _db.AttendanceRecords
            .FirstOrDefaultAsync(a => a.StudentId == student.Id && a.CourseId == course.Id && a.Date == today);

        if (record is not null && record.Status == AttendanceStatus.Present)
        {
            return Ok(new
            {
                matched = true,
                alreadyMarked = true,
                studentName = student.FullName,
                rollNumber = student.RollNumber,
                score = Math.Round(score, 2)
            });
        }

        if (record is null)
        {
            record = new AttendanceRecord
            {
                StudentId = student.Id,
                CourseId = course.Id,
                Date = today
            };
            _db.AttendanceRecords.Add(record);
        }

        record.Status = AttendanceStatus.Present;
        record.MarkedAt = DateTime.Now;
        record.MarkedBy = "FaceRecognition";
        await _db.SaveChangesAsync();

        return Ok(new
        {
            matched = true,
            alreadyMarked = false,
            studentName = student.FullName,
            rollNumber = student.RollNumber,
            time = record.MarkedAt?.ToString("HH:mm:ss"),
            score = Math.Round(score, 2)
        });
    }

    // Returns the course only if it belongs to the logged-in faculty member.
    private async Task<Course?> GetOwnCourseAsync(int courseId)
    {
        var userId = _userManager.GetUserId(User);
        return await _db.Courses
            .FirstOrDefaultAsync(c => c.Id == courseId && c.Faculty != null && c.Faculty.UserId == userId);
    }

    // The browser sends "data:image/jpeg;base64,XXXX" - keep only the part after the comma.
    private static byte[]? DecodeImage(string? dataUrl)
    {
        if (string.IsNullOrWhiteSpace(dataUrl))
        {
            return null;
        }

        try
        {
            var base64 = dataUrl.Contains(',') ? dataUrl[(dataUrl.IndexOf(',') + 1)..] : dataUrl;
            return Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}

public record MarkAttendanceRequest(int CourseId, string Image);