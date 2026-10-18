namespace AttendancePortal.Models;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Faculty = "Faculty";
    public const string Student = "Student";

    public static readonly string[] All = { Admin, Faculty, Student };
}