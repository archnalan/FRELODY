using System.ComponentModel.DataAnnotations;

namespace FRELODYAPP.Models.SubModels
{
    public class BaseEntity : IBaseEntity
    {

        public DateTime? DateCreated { get; set; }
        public DateTime? DateModified { get; set; }
        public bool? IsDeleted { get; set; }

        [StringLength(255)]
        public string? ModifiedBy { get; set; }

        [StringLength(255)]
        public virtual Guid? TenantId { get; set; } 
    }
    public interface IBaseEntity
    {
        DateTime? DateCreated { get; set; }
        DateTime? DateModified { get; set; }
        bool? IsDeleted { get; set; }
        string? ModifiedBy { get; set; }
        Guid? TenantId { get; set; }
    }    
}
