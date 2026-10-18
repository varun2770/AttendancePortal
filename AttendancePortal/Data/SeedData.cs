using AttendancePortal.Models;
using AttendancePortal.Models.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AttendancePortal.Data;

public static class SeedData
{
    private const string AdminEmail = "admin@attendanceportal.local";
    private const string AdminPassword = "Admin@12345";

    private const string DemoStudentEmail = "student@attendanceportal.local";
    private const string DemoStudentPassword = "Student@12345";

    private const string DemoFacultyEmail = "faculty@attendanceportal.local";
    private const string DemoFacultyPassword = "Faculty@12345";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Admin account
        var adminUser = await userManager.FindByEmailAsync(AdminEmail);
        if (adminUser is null)
        {
            adminUser = new IdentityUser
            {
                UserName = AdminEmail,
                Email = AdminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, AdminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, Roles.Admin);
            }
        }

        // Demo course
        var course = await db.Courses.FirstOrDefaultAsync(c => c.CourseCode == "CSE101");
        if (course is null)
        {
            course = new Course { CourseCode = "CSE101", CourseName = "Introduction to Programming" };
            db.Courses.Add(course);
            await db.SaveChangesAsync();
        }

        // Demo faculty account + profile, assigned to the demo course
        var facultyUser = await userManager.FindByEmailAsync(DemoFacultyEmail);
        if (facultyUser is null)
        {
            var created = new IdentityUser
            {
                UserName = DemoFacultyEmail,
                Email = DemoFacultyEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(created, DemoFacultyPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(created, Roles.Faculty);
                facultyUser = created;
            }
        }

        if (facultyUser is not null)
        {
            var facultyProfile = await db.Faculties.FirstOrDefaultAsync(f => f.UserId == facultyUser.Id);
            if (facultyProfile is null)
            {
                facultyProfile = new Faculty
                {
                    UserId = facultyUser.Id,
                    FullName = "Demo Faculty",
                    Department = "Computer Science"
                };
                db.Faculties.Add(facultyProfile);
                await db.SaveChangesAsync();
            }

            if (course.FacultyId is null)
            {
                course.FacultyId = facultyProfile.Id;
                await db.SaveChangesAsync();
            }
        }

        // Demo student account + profile
        var studentUser = await userManager.FindByEmailAsync(DemoStudentEmail);
        if (studentUser is null)
        {
            var created = new IdentityUser
            {
                UserName = DemoStudentEmail,
                Email = DemoStudentEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(created, DemoStudentPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(created, Roles.Student);
                studentUser = created;
            }
        }

        if (studentUser is not null && !await db.Students.AnyAsync(s => s.UserId == studentUser.Id))
        {
            db.Students.Add(new Student
            {
                UserId = studentUser.Id,
                FullName = "Demo Student",
                RollNumber = "S001",
                CourseId = course.Id
            });
            await db.SaveChangesAsync();
        }
    }
}