using System.ComponentModel.DataAnnotations;

namespace FRELODYSHRD.Dtos.Org
{
    /// <summary>
    /// Finalize a user-only registration (no organization). Used by the simplified
    /// 3-step Register flow which never creates a tenant. Sets username + password
    /// on the previously-created (email-verified) <see cref="UserId"/>.
    /// </summary>
    public class CompleteUserRegistrationDto
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MinLength(4)]
        public string Password { get; set; } = string.Empty;

        public string? UserName { get; set; }
    }
}
