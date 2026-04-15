using FRELODYAPP.Dtos.SubDtos;
using System.ComponentModel.DataAnnotations;

namespace FRELODYSHRD.Dtos.CreateDtos
{
    public class TenantCreateDto
    {
        public string TenantName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? Email { get; set; }
        public string? UserFullName { get; set; }
        public string? UserEmail { get; set; }

        [Required]
        [MinLength(4)]
        public string Password { get; set; } = string.Empty;
    }

    public class CompleteTenantRegistrationDto
    {
        [Required]
        public string TenantId { get; set; } = string.Empty;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MinLength(4)]
        public string Password { get; set; } = string.Empty;

        public string? UserName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? TenantName { get; set; }
    }
}
