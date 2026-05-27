using System.ComponentModel.DataAnnotations;

namespace FRELODYSHRD.Dtos.AuthDtos
{
    /// <summary>
    /// Payload from Google Identity Services One Tap / button: a signed ID-token
    /// <c>credential</c> (a JWT), not an authorization code. Verified server-side.
    /// </summary>
    public class GoogleOneTapRequestDto
    {
        [Required]
        [MinLength(10)]
        public string Credential { get; set; } = string.Empty;
    }
}
