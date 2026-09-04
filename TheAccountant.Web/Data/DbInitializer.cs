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
            var password = configuration["SeedUser:Password"];

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    "Seed user credentials have not been configured.");
            }

            var existingUser = await userManager.FindByNameAsync(username);

            if (existingUser is not null)
            {
                return;
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