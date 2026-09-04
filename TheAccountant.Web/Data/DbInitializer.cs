using Microsoft.AspNetCore.Identity;
using TheAccountant.Web.Models;

namespace TheAccountant.Web.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(
    IServiceProvider services,
    IConfiguration configuration)
        {
            using var scope = services.CreateScope();

            var userManager =
                scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var username = configuration["SeedUser:Username"];

            if (string.IsNullOrWhiteSpace(username))
            {
                throw new InvalidOperationException(
                    "Seed username has not been configured.");
            }

            // Check for the user FIRST.
            var existingUser = await userManager.FindByNameAsync(username);

            if (existingUser is not null)
            {
                return;
            }

            // Password is only needed if we actually have to create the user.
            var password = configuration["SeedUser:Password"];

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    "Seed password has not been configured.");
            }

            var user = new ApplicationUser
            {
                UserName = username
            };

            var result = await userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                var errors = string.Join(
                    Environment.NewLine,
                    result.Errors.Select(e => e.Description));

                throw new InvalidOperationException(
                    $"Unable to create seed user:{Environment.NewLine}{errors}");
            }
        }
    }
}