using DocumentFormat.OpenXml.Math;
using FRELODYSHRD.Dtos.SubDtos;
using System.ComponentModel.DataAnnotations;

namespace FRELODYAPP.Dtos.SubDtos
{
    public class BaseEntityDto : IBaseEntityDto
    {
        public string? Id { get; set; }
        public DateTimeOffset? DateCreated { get; set; }
        public DateTimeOffset? DateModified { get; set; }
        public string? CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }
        public bool? IsDeleted { get; set; }
        public Access? Access { get; set; }
        public virtual string? TenantId { get; set; }
    }
    public interface IBaseEntityDto
    {
        public string? Id { get; set; }
        public DateTimeOffset? DateCreated { get; set; }
        public DateTimeOffset? DateModified { get; set; }
        public Access? Access { get; set; }
        public bool? IsDeleted { get; set; }
        public string? CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }
        public string? TenantId { get; set; }
    }
}
