using FRELODYAPP.Models.SubModels;
using FRELODYSHRD.Dtos.SubDtos;
using System.ComponentModel.DataAnnotations;

namespace FRELODYAPP.Models
{
    public class Tenant 
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
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
        public DateTime? DateCreated { get; set; }
        public DateTime? DateModified { get; set; }
        public bool? IsDeleted { get; set; }
        public Access? Access { get; set; }

        [StringLength(255)]
        public string? CreatedBy { get; set; }

        [StringLength(255)]
        public string? ModifiedBy { get; set; }
    }
}
