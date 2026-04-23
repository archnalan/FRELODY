using System.ComponentModel.DataAnnotations;

namespace FRELODYSHRD.Dtos.AuthDtos
{
    /// <summary>
    /// Authenticated self-service password change. Used both for the
    /// "first-login forced change" flow (after admin creates the account)
    /// and for normal in-app password updates.
    /// </summary>
    public class ChangeOwnPasswordDto
    {
        [Required, MinLength(1)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required, MinLength(8)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
