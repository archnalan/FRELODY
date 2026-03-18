using System.ComponentModel.DataAnnotations;

namespace FRELODYSHRD.Dtos.AuthDtos
{
    public class GoogleAuthRequestDto
    {
        [Required]
        [MinLength(3)]
        public string Code { get; set; } = string.Empty;
        [Required]
        [MinLength(3)]
        public string CodeVerifier { get; set; } = string.Empty;
    }
}
