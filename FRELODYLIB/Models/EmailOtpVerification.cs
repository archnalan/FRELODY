using FRELODYAPP.Models.SubModels;
using System.ComponentModel.DataAnnotations;

namespace FRELODYLIB.Models
{
    public class EmailOtpVerification
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string OtpCode { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset ExpiresAt { get; set; }

        public bool IsUsed { get; set; }

        public int AttemptCount { get; set; }

        /// <summary>
        /// The tenant ID created during the initial registration step (before email verification).
        /// </summary>
        [StringLength(450)]
        public string? TenantId { get; set; }

        /// <summary>
        /// The user ID created during the initial registration step.
        /// </summary>
        [StringLength(450)]
        public string? UserId { get; set; }
    }
}
