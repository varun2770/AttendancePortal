using AttendancePortal.Data;
using AttendancePortal.Models;
using AttendancePortal.Models.Domain;
using AttendancePortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendancePortal.Controllers;

[Authorize(Roles = Roles.Student)]
public class StudentController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public StudentController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var student = await GetCurrentStudentAsync();
        return View(student);
    }

    [HttpGet]
    public async Task<IActionResult> RegisterFace()
    {
        var student = await GetCurrentStudentAsync();
        if (student is null)
        {
            return RedirectToAction(nameof(Index));
        }

        return View(student);
    }

    // Called by the webcam page: receives one photo, turns the face into a fingerprint, saves it.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterFace([FromBody] FaceImageRequest request, [FromServices] FaceService faceService)
    {
        var student = await GetCurrentStudentAsync();
        if (student is null)
        {
            return NotFound(new { message = "No student profile is linked to this account." });
        }

        if (string.IsNullOrWhiteSpace(request.Image))
        {
            return BadRequest(new { message = "No image was received." });
        }

        byte[] imageBytes;
        try
        {
            // The browser sends "data:image/jpeg;base64,XXXX" - keep only the part after the comma.
            var base64 = request.Image.Contains(',')
                ? request.Image[(request.Image.IndexOf(',') + 1)..]
                : request.Image;
            imageBytes = Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            return BadRequest(new { message = "The image data was not valid." });
        }

        var (embedding, error) = faceService.GetEmbedding(imageBytes);
        if (embedding is null)
        {
            return BadRequest(new { message = error });
        }

        student.FaceEmbedding = FaceService.ToBytes(embedding);
        student.FaceRegistered = true;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Face registered successfully. You can go back to your dashboard." });
    }

    private async Task<Student?> GetCurrentStudentAsync()
    {
        var userId = _userManager.GetUserId(User);
        return await _db.Students
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.UserId == userId);
    }
}

public record FaceImageRequest(string Image);