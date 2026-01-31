using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using ResourceEngagementTrackingSystem.Infrastructure.Models;

namespace ResourceEngagementTrackingSystem.Infrastructure.Services;

public class RoleSeederService
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public RoleSeederService(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager
    )
    {
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task SeedRolesAsync()
    {
        foreach (var role in UserRoles.AllRoles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    public async Task SeedDefaultAdminAsync(string email, string password)
    {
        var adminUser = await _userManager.FindByEmailAsync(email);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = "Admin",
                LastName = "User",
                EmailConfirmed = true,
            };

            var result = await _userManager.CreateAsync(adminUser, password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(adminUser, UserRoles.Admin);
            }
        }
        else if (!await _userManager.IsInRoleAsync(adminUser, UserRoles.Admin))
        {
            await _userManager.AddToRoleAsync(adminUser, UserRoles.Admin);
        }
    }
}
