using Microsoft.AspNetCore.Identity;

namespace Patients.Api.Infrastructure.Security;

public static class IdentitySeed
{
    private const string ReaderRole = "Reader";
    private const string EditorRole = "Editor";

    public static async Task SeedRolesAndUsersAsync(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        await EnsureRoleAsync(roleManager, ReaderRole);
        await EnsureRoleAsync(roleManager, EditorRole);

        await EnsureUserAsync(userManager, "reader", "Reader123!", ReaderRole);
        await EnsureUserAsync(userManager, "editor", "Editor123!", EditorRole);
    }

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    private static async Task EnsureUserAsync(
        UserManager<IdentityUser> userManager,
        string userName,
        string password,
        string role)
    {
        var user = await userManager.FindByNameAsync(userName);
        if (user is null)
        {
            user = new IdentityUser { UserName = userName };
            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                return;
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }
    }
}
