using System;

namespace FRELODYSHRD.Dtos.Org
{
    /// <summary>
    /// User-facing projection of the underlying <c>Tenant</c> entity. The backend
    /// keeps the <c>Tenant</c> name internally for backward compatibility; the UI
    /// only ever speaks "Organization".
    /// </summary>
    public class OrganizationDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
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
        public DateTime? DateCreated { get; set; }
        public int MemberCount { get; set; }
    }
}
