using DocumentFormat.OpenXml.Wordprocessing;
using FRELODYAPP.Dtos.SubDtos;

namespace FRELODYAPP.Dtos
{
    public class TenantDto : BaseEntityDto
    {
        public Guid? TenantId { get; set; }
        public string TenantName { get; set; } 
        public string? Address { get; set; }  
        public string? City { get; set; }  
        public string? State { get; set; } 
        public string? PostalCode { get; set; } 
        public string? Country { get; set; } 
        public string? PhoneNumber { get; set; }  
        public string? Email { get; set; }  
        public string? Website { get; set; }  
        public string? Industry { get; set; }         
        public string? BusinessRegNumber { get; set; }
        public string? TaxIdentificationNumber { get; set; }
    }
}
