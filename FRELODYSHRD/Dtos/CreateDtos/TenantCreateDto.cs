using FRELODYAPP.Dtos.SubDtos;

namespace FRELODYSHRD.Dtos.CreateDtos
{
    public class TenantCreateDto
    {
        public string TenantName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? Email { get; set; }
    }
}
