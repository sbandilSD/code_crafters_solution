using Microsoft.AspNetCore.Identity;

namespace ResourceEngagementTrackingSystem.Infrastructure.Models;

public class ApplicationUser : IdentityUser
{
    public string Title { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
}