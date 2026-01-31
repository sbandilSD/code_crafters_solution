namespace ResourceEngagementTrackingSystem.Infrastructure.Models;

public static class UserRoles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Employee = "Employee";
    public const string User = "User";
    
    public static readonly string[] AllRoles = { Admin, Manager, Employee, User };
}