using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace ResourceEngagementTrackingSystem.Application.DTOs;

public class AssignRoleDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string Role { get; set; } = string.Empty;
}

public class RemoveRoleDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string Role { get; set; } = string.Empty;
}

public class UserRolesResponseDto
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}