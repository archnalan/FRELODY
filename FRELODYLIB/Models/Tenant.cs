using FRELODYAPP.Models.SubModels;
using System.ComponentModel.DataAnnotations;

namespace FRELODYAPP.Models
{
    public class Tenant : BaseEntity
    {
        [Key]
        public Guid TenantId { get; set; }
        public string TenantName { get; set; }
        public string? BusinessRegNumber { get; set; }
        public string? TaxIdentificationNumber { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? Industry { get; set; }
    }
}
