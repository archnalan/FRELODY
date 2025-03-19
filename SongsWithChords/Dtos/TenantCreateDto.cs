using SongsWithChords.Dtos.SubDtos;

namespace SongsWithChords.Dtos
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
