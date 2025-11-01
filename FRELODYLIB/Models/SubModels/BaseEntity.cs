using FRELODYSHRD.Dtos.SubDtos;
using System.ComponentModel.DataAnnotations;

namespace FRELODYAPP.Models.SubModels
{
    public class BaseEntity : IBaseEntity
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? DateCreated { get; set; }
        public DateTimeOffset? DateModified { get; set; }
        public bool? IsDeleted { get; set; }
        public Access? Access { get; set; }

        [StringLength(255)]
        public string? CreatedBy { get; set; }

        [StringLength(255)]
        public string? ModifiedBy { get; set; }

        [StringLength(450)] //From the Primary key length
        public virtual string? TenantId { get; set; } 
    }
    public interface IBaseEntity
    {
        DateTimeOffset? DateCreated { get; set; }
        DateTimeOffset? DateModified { get; set; }
        bool? IsDeleted { get; set; }
        string? CreatedBy { get; set; }
        string? ModifiedBy { get; set; }
        string? TenantId { get; set; }
    }  
}
