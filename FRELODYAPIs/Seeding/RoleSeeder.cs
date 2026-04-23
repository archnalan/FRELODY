using FRELODYSHRD.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace FRELODYAPIs.Seeding
{
    /// <summary>
    /// Ensures every platform + organization role declared in
    /// <see cref="UserRoles.AllRoles"/> exists in ASP.NET Identity.
    /// </summary>
    public class RoleSeeder
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<RoleSeeder> _logger;

        public RoleSeeder(RoleManager<IdentityRole> roleManager, ILogger<RoleSeeder> logger)
        {
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            foreach (var role in UserRoles.AllRoles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    var result = await _roleManager.CreateAsync(new IdentityRole(role));
                    if (result.Succeeded)
                        _logger.LogInformation("Seeded missing identity role: {Role}", role);
                    else
                        _logger.LogWarning("Failed to seed role {Role}: {Errors}", role,
                            string.Join("; ", result.Errors));
                }
            }
        }
    }
}
