using System;
using System.Linq;
using System.Threading.Tasks;
using ResourceEngagementTrackingSystem.Application.DTOs;
using ResourceEngagementTrackingSystem.Infrastructure.Models;
using ResourceEngagementTrackingSystem.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly TokenService _tokenService;
    private readonly TwoFactorService _twoFactorService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        TokenService tokenService,
        TwoFactorService twoFactorService
    )
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _twoFactorService = twoFactorService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
            return BadRequest(new { message = "User with this email already exists" });

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            Title = dto.Title,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            PhoneNumber = dto.PhoneNumber,
            Address = dto.Address,
            ZipCode = dto.ZipCode,
        };
        var result = await _userManager.CreateAsync(user, dto.Password);

        if (!result.Succeeded)
            return BadRequest(new { message = "Registration failed", errors = result.Errors });

        // Assign role - validate role exists and assign default if invalid
        var roleToAssign = UserRoles.AllRoles.Contains(dto.Role) ? dto.Role : UserRoles.User;
        await _userManager.AddToRoleAsync(user, roleToAssign);

        return Ok(new { message = "User registered successfully", userId = user.Id, role = roleToAssign });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        // Find user by email
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
            return Unauthorized(new { message = "Invalid email or password" });

        // Check password
        var isPasswordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!isPasswordValid)
            return Unauthorized(new { message = "Invalid email or password" });

        // Check if user is locked out
        if (await _userManager.IsLockedOutAsync(user))
            return BadRequest(new { message = "Account is locked out" });

        // Check if 2FA is enabled
        var is2faEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
        if (is2faEnabled)
        {
            return Ok(
                new
                {
                    message = "Two-factor authentication required",
                    requiresTwoFactor = true,
                    email = user.Email,
                }
            );
        }

        // Generate JWT token (only if 2FA is not enabled)
        var token = await _tokenService.GenerateTokenAsync(user);
        var expiresAt = _tokenService.GetTokenExpiration();
        var userRoles = await _userManager.GetRolesAsync(user);

        var response = new LoginResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
        };

        return Ok(new { 
            token = response.Token,
            expiresAt = response.ExpiresAt,
            userId = response.UserId,
            email = response.Email,
            firstName = response.FirstName,
            lastName = response.LastName,
            roles = userRoles
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        // In JWT, logout is handled client-side by removing the token
        // Optionally, you could implement token blacklisting here
        return Ok(
            new { message = "Logout successful. Please remove the token from client storage." }
        );
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found" });

        return Ok(
            new
            {
                userId = user.Id,
                email = user.Email,
                title = user.Title,
                firstName = user.FirstName,
                lastName = user.LastName,
                phoneNumber = user.PhoneNumber,
                address = user.Address,
                zipCode = user.ZipCode,
                twoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user),
            }
        );
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found" });

        // Update user properties
        user.Title = dto.Title;
        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.Address = dto.Address;
        user.ZipCode = dto.ZipCode;
        user.PhoneNumber = dto.PhoneNumber;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(
                new
                {
                    message = "Failed to update profile",
                    errors = result.Errors.Select(e => e.Description),
                }
            );
        }

        return Ok(
            new
            {
                message = "Profile updated successfully",
                profile = new
                {
                    userId = user.Id,
                    email = user.Email,
                    title = user.Title,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    phoneNumber = user.PhoneNumber,
                    address = user.Address,
                    zipCode = user.ZipCode,
                    twoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user),
                },
            }
        );
    }

    [HttpGet("2fa/setup")]
    [Authorize]
    public async Task<IActionResult> GetTwoFactorSetup()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found" });

        var (qrCodeUri, qrCodeImage, manualKey) = await _twoFactorService.GenerateQrCodeDataAsync(
            user
        );

        return Ok(
            new TwoFactorSetupDto
            {
                QrCodeUri = qrCodeUri,
                QrCodeImage = qrCodeImage,
                ManualEntryKey = manualKey,
            }
        );
    }

    [HttpPost("2fa/enable")]
    [Authorize]
    public async Task<IActionResult> EnableTwoFactor(EnableTwoFactorDto dto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found" });

        await _userManager.SetTwoFactorEnabledAsync(user, dto.Enable);

        return Ok(
            new
            {
                message = dto.Enable
                    ? "Two-factor authentication enabled"
                    : "Two-factor authentication disabled",
                twoFactorEnabled = dto.Enable,
            }
        );
    }

    [HttpPost("2fa/verify")]
    public async Task<IActionResult> VerifyTwoFactor(VerifyTwoFactorDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
            return Unauthorized(new { message = "Invalid verification request" });

        var isValid = await _twoFactorService.VerifyTwoFactorCodeAsync(user, dto.Code);
        if (!isValid)
            return Unauthorized(new { message = "Invalid verification code" });

        // Generate JWT token after successful 2FA verification
        var token = await _tokenService.GenerateTokenAsync(user);
        var expiresAt = _tokenService.GetTokenExpiration();
        var userRoles = await _userManager.GetRolesAsync(user);

        var response = new LoginResponseDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
        };

        return Ok(new { 
            token = response.Token,
            expiresAt = response.ExpiresAt,
            userId = response.UserId,
            email = response.Email,
            firstName = response.FirstName,
            lastName = response.LastName,
            roles = userRoles
        });
    }

    [HttpGet("2fa/status")]
    [Authorize]
    public async Task<IActionResult> GetTwoFactorStatus()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found" });

        var is2faEnabled = await _userManager.GetTwoFactorEnabledAsync(user);

        return Ok(new { twoFactorEnabled = is2faEnabled });
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found" });

        // Verify current password
        var isCurrentPasswordValid = await _userManager.CheckPasswordAsync(
            user,
            dto.CurrentPassword
        );
        if (!isCurrentPasswordValid)
            return BadRequest(new { message = "Current password is incorrect" });

        // Change password
        var result = await _userManager.ChangePasswordAsync(
            user,
            dto.CurrentPassword,
            dto.NewPassword
        );
        if (!result.Succeeded)
        {
            return BadRequest(
                new
                {
                    message = "Failed to change password",
                    errors = result.Errors.Select(e => e.Description),
                }
            );
        }

        return Ok(new { message = "Password changed successfully" });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
        {
            // Don't reveal that the user does not exist
            return Ok(
                new { message = "If the email exists, a password reset link has been sent." }
            );
        }

        // Generate password reset token
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        // Check if 2FA is enabled to inform the client
        var is2faEnabled = await _userManager.GetTwoFactorEnabledAsync(user);

        // In a real application, you would send an email here
        // For demo purposes, we'll return the token (DO NOT do this in production)
        return Ok(
            new
            {
                message = "Password reset token generated. In production, this would be sent via email.",
                email = dto.Email,
                resetToken = token, // Remove this in production!
                requiresTwoFactor = is2faEnabled,
            }
        );
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
            return BadRequest(new { message = "Invalid reset request" });

        // Check if 2FA is enabled for the user
        var is2faEnabled = await _userManager.GetTwoFactorEnabledAsync(user);

        if (is2faEnabled)
        {
            // If 2FA is enabled, require 2FA code
            if (string.IsNullOrEmpty(dto.TwoFactorCode))
            {
                return BadRequest(
                    new
                    {
                        message = "Two-factor authentication code is required",
                        requiresTwoFactor = true,
                    }
                );
            }

            // Verify 2FA code
            var isValidTwoFactorCode = await _twoFactorService.VerifyTwoFactorCodeAsync(
                user,
                dto.TwoFactorCode
            );
            if (!isValidTwoFactorCode)
            {
                return BadRequest(new { message = "Invalid two-factor authentication code" });
            }
        }

        // Reset password using the token
        var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(
                new
                {
                    message = "Failed to reset password",
                    errors = result.Errors.Select(e => e.Description),
                }
            );
        }

        return Ok(new { message = "Password reset successfully" });
    }

    // Sample role-based endpoints for demonstration
    [HttpGet("admin-only")]
    [Authorize(Roles = UserRoles.Admin)]
    public IActionResult AdminOnly()
    {
        return Ok(new { message = "This endpoint is only accessible by Admins", timestamp = DateTime.UtcNow });
    }

    [HttpGet("manager-and-admin")]
    [Authorize(Roles = UserRoles.Admin + "," + UserRoles.Manager)]
    public IActionResult ManagerAndAdmin()
    {
        return Ok(new { message = "This endpoint is accessible by Admins and Managers", timestamp = DateTime.UtcNow });
    }

    [HttpGet("authenticated-users")]
    [Authorize] // Any authenticated user
    public async Task<IActionResult> AuthenticatedUsers()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var user = await _userManager.FindByIdAsync(userId!);
        var userRoles = await _userManager.GetRolesAsync(user!);
        
        return Ok(new { 
            message = "This endpoint is accessible by any authenticated user", 
            user = user?.Email,
            roles = userRoles,
            timestamp = DateTime.UtcNow 
        });
    }
}
