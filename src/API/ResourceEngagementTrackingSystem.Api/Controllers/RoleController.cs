using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ResourceEngagementTrackingSystem.Application.DTOs;
using ResourceEngagementTrackingSystem.Infrastructure.Models;

namespace ResourceEngagementTrackingSystem.Api.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize] // Require authentication for all endpoints
public class RoleController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public RoleController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager
    )
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet("available")]
    [Authorize(Roles = UserRoles.Admin + "," + UserRoles.Manager)]
    public IActionResult GetAvailableRoles()
    {
        return Ok(UserRoles.AllRoles);
    }

    [HttpPost("assign")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> AssignRole(AssignRoleDto dto)
    {
        if (!UserRoles.AllRoles.Contains(dto.Role))
        {
            return BadRequest(new { message = "Invalid role specified." });
        }

        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        if (await _userManager.IsInRoleAsync(user, dto.Role))
        {
            return BadRequest(new { message = $"User already has the {dto.Role} role." });
        }

        var result = await _userManager.AddToRoleAsync(user, dto.Role);
        if (result.Succeeded)
        {
            return Ok(new { message = $"Role {dto.Role} assigned to {dto.Email} successfully." });
        }

        return BadRequest(new { message = "Failed to assign role.", errors = result.Errors });
    }

    [HttpPost("remove")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> RemoveRole(RemoveRoleDto dto)
    {
        if (!UserRoles.AllRoles.Contains(dto.Role))
        {
            return BadRequest(new { message = "Invalid role specified." });
        }

        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        if (!await _userManager.IsInRoleAsync(user, dto.Role))
        {
            return BadRequest(new { message = $"User does not have the {dto.Role} role." });
        }

        var result = await _userManager.RemoveFromRoleAsync(user, dto.Role);
        if (result.Succeeded)
        {
            return Ok(new { message = $"Role {dto.Role} removed from {dto.Email} successfully." });
        }

        return BadRequest(new { message = "Failed to remove role.", errors = result.Errors });
    }

    [HttpGet("user/{email}")]
    [Authorize(Roles = UserRoles.Admin + "," + UserRoles.Manager)]
    public async Task<IActionResult> GetUserRoles(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var response = new UserRolesResponseDto
        {
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = roles.ToList(),
        };

        return Ok(response);
    }

    [HttpGet("users")]
    [Authorize(Roles = UserRoles.Admin + "," + UserRoles.Manager)]
    public async Task<IActionResult> GetAllUsersWithRoles()
    {
        var users = _userManager.Users.ToList();
        var usersWithRoles = new List<UserRolesResponseDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            usersWithRoles.Add(
                new UserRolesResponseDto
                {
                    UserId = user.Id,
                    Email = user.Email!,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Roles = roles.ToList(),
                }
            );
        }

        return Ok(usersWithRoles);
    }

    [HttpGet("my-roles")]
    [Authorize]
    public async Task<IActionResult> GetMyRoles()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new { roles });
    }
}
