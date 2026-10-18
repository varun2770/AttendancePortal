using AttendancePortal.Models;
using AttendancePortal.Models.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendancePortal.Controllers;

public partial class AdminController
{
    private UserManager<IdentityUser> UserMgr =>
        HttpContext.RequestServices.GetRequiredService<UserManager<IdentityUser>>();

    public async Task<IActionResult> FacultyList()
    {
        var list = await _db.Faculties
            .Include(f => f.Courses)
            .OrderBy(f => f.FullName)
            .ToListAsync();

        var emails = new Dictionary<int, string>();
        foreach (var f in list)
        {
            var user = await UserMgr.FindByIdAsync(f.UserId);
            emails[f.Id] = user?.Email ?? "";
        }

        ViewBag.Emails = emails;
        return View(list);
    }

    [HttpGet]
    public IActionResult CreateFaculty()
    {
        return View("FacultyForm", new FacultyFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFaculty(FacultyFormViewModel model)
    {
        model.Email = model.Email?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), "Password is required.");
        }

        if (!ModelState.IsValid)
        {
            return View("FacultyForm", model);
        }

        if (await UserMgr.FindByEmailAsync(model.Email) is not null)
        {
            ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
            return View("FacultyForm", model);
        }

        var user = new IdentityUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true
        };

        var result = await UserMgr.CreateAsync(user, model.Password!);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
            {
                ModelState.AddModelError(string.Empty, e.Description);
            }
            return View("FacultyForm", model);
        }

        await UserMgr.AddToRoleAsync(user, Roles.Faculty);

        _db.Faculties.Add(new Faculty
        {
            UserId = user.Id,
            FullName = model.FullName.Trim(),
            Department = model.Department.Trim()
        });
        await _db.SaveChangesAsync();

        TempData["Message"] = "Faculty account created.";
        return RedirectToAction(nameof(FacultyList));
    }

    [HttpGet]
    public async Task<IActionResult> EditFaculty(int id)
    {
        var faculty = await _db.Faculties.FindAsync(id);
        if (faculty is null) return NotFound();

        var user = await UserMgr.FindByIdAsync(faculty.UserId);

        return View("FacultyForm", new FacultyFormViewModel
        {
            Id = faculty.Id,
            Email = user?.Email ?? "",
            FullName = faculty.FullName,
            Department = faculty.Department
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditFaculty(FacultyFormViewModel model)
    {
        var faculty = await _db.Faculties.FindAsync(model.Id);
        if (faculty is null) return NotFound();

        // Email and password aren't edited here.
        ModelState.Remove(nameof(model.Email));
        ModelState.Remove(nameof(model.Password));

        if (!ModelState.IsValid)
        {
            var user = await UserMgr.FindByIdAsync(faculty.UserId);
            model.Email = user?.Email ?? "";
            return View("FacultyForm", model);
        }

        faculty.FullName = model.FullName.Trim();
        faculty.Department = model.Department.Trim();
        await _db.SaveChangesAsync();

        TempData["Message"] = "Faculty updated.";
        return RedirectToAction(nameof(FacultyList));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFaculty(int id)
    {
        var faculty = await _db.Faculties.FindAsync(id);
        if (faculty is null) return NotFound();

        if (await _db.Courses.AnyAsync(c => c.FacultyId == id))
        {
            TempData["Error"] = "This faculty member is assigned to a course. Reassign or unassign the course first.";
            return RedirectToAction(nameof(FacultyList));
        }

        var user = await UserMgr.FindByIdAsync(faculty.UserId);

        _db.Faculties.Remove(faculty);
        await _db.SaveChangesAsync();

        if (user is not null)
        {
            await UserMgr.DeleteAsync(user);
        }

        TempData["Message"] = "Faculty deleted.";
        return RedirectToAction(nameof(FacultyList));
    }
}