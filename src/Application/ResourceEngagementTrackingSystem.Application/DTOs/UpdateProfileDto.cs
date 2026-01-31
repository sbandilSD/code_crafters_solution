using System.ComponentModel.DataAnnotations;

namespace ResourceEngagementTrackingSystem.Application.DTOs;

public class UpdateProfileDto
{
    [StringLength(50, ErrorMessage = "Title must be less than 50 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "First name is required")]
    [StringLength(100, ErrorMessage = "First name must be less than 100 characters")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100, ErrorMessage = "Last name must be less than 100 characters")]
    public string LastName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid phone number format")]
    [StringLength(20, ErrorMessage = "Phone number must be less than 20 characters")]
    public string PhoneNumber { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Address must be less than 200 characters")]
    public string Address { get; set; } = string.Empty;

    [StringLength(10, ErrorMessage = "Zip code must be less than 10 characters")]
    public string ZipCode { get; set; } = string.Empty;
}