using FRELODYAPP.Models.SubModels;
using FRELODYSHRD.Constants;
using FRELODYSHRD.Dtos.UserDtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace FRELODYAPIs.Seeding
{
    /// <summary>
    /// Idempotently elevates the user identified by the
    /// <c>SUPERADMIN_SEED_EMAIL</c> environment variable to platform-tier
    /// <see cref="UserRoles.SuperAdmin"/> on every startup. Logs a warning if
    /// the email is configured but no matching user exists yet (the next
    /// startup will succeed once that user registers).
    /// </summary>
    public class SuperAdminSeeder
    {
        public const string EnvVarName = "SUPERADMIN_SEED_EMAIL";

        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _config;
        private readonly ILogger<SuperAdminSeeder> _logger;

        public SuperAdminSeeder(UserManager<User> userManager, IConfiguration config, ILogger<SuperAdminSeeder> logger)
        {
            _userManager = userManager;
            _config = config;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            // Prefer environment variable, fall back to configuration key.
            var email = System.Environment.GetEnvironmentVariable(EnvVarName)
                        ?? _config[EnvVarName]
                        ?? _config["SuperAdmin:SeedEmail"];

            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogInformation(
                    "{Var} not set; skipping SuperAdmin seed.", EnvVarName);
                return;
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning(
                    "{Var}={Email} configured but no matching user exists. Will retry on next startup.",
                    EnvVarName, email);
                return;
            }

            var changed = false;
            if (user.UserType != UserType.SuperAdmin)
            {
                user.UserType = UserType.SuperAdmin;
                changed = true;
            }
            if (changed)
            {
                await _userManager.UpdateAsync(user);
            }

            if (!await _userManager.IsInRoleAsync(user, UserRoles.SuperAdmin))
            {
                var result = await _userManager.AddToRoleAsync(user, UserRoles.SuperAdmin);
                if (result.Succeeded)
                {
                    _logger.LogInformation(
                        "Elevated user {Email} to SuperAdmin via {Var}.", email, EnvVarName);
                }
                else
                {
                    _logger.LogError(
                        "Failed to add SuperAdmin role to {Email}: {Errors}",
                        email, string.Join("; ", result.Errors));
                }
            }
            else
            {
                _logger.LogDebug("User {Email} is already SuperAdmin.", email);
            }
        }
    }
}
