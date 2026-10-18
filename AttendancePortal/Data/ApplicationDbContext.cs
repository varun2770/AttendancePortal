using AttendancePortal.Models.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AttendancePortal.Data;

public class ApplicationDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<Faculty> Faculties => Set<Faculty>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Student>().HasIndex(s => s.RollNumber).IsUnique();
        builder.Entity<Student>().HasIndex(s => s.UserId).IsUnique();
        builder.Entity<Faculty>().HasIndex(f => f.UserId).IsUnique();
        builder.Entity<Course>().HasIndex(c => c.CourseCode).IsUnique();

        builder.Entity<AttendanceRecord>()
            .HasIndex(a => new { a.StudentId, a.CourseId, a.Date })
            .IsUnique();

        builder.Entity<AttendanceRecord>()
            .HasOne(a => a.Student)
            .WithMany(s => s.AttendanceRecords)
            .HasForeignKey(a => a.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<AttendanceRecord>()
            .HasOne(a => a.Course)
            .WithMany(c => c.AttendanceRecords)
            .HasForeignKey(a => a.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Course>()
            .HasOne(c => c.Faculty)
            .WithMany(f => f.Courses)
            .HasForeignKey(c => c.FacultyId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Student>()
            .HasOne(s => s.Course)
            .WithMany(c => c.Students)
            .HasForeignKey(s => s.CourseId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}