using DocumentFormat.OpenXml.Math;
using FRELODYSHRD.Dtos.SubDtos;
using System.ComponentModel.DataAnnotations;

namespace FRELODYAPP.Dtos.SubDtos
{
    public class BaseEntityDto : IBaseEntityDto
    {
        public string Id { get; set; }
        public DateTime? DateCreated { get; set; }
        public DateTime? DateModified { get; set; }
        public Access? Access { get; set; }

        [StringLength(255)]
        public virtual string? TenantId { get; set; }
    }
    public interface IBaseEntityDto
    {
        DateTime? DateCreated { get; set; }
        DateTime? DateModified { get; set; }
        string? TenantId { get; set; }
    }
}
