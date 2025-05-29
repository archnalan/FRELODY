using System.ComponentModel.DataAnnotations;

namespace FRELODYAPP.Models.SubModels
{
    public class BaseEntity : IBaseEntity
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime? DateCreated { get; set; }
        public DateTime? DateModified { get; set; }
        public bool? IsDeleted { get; set; }

        [StringLength(255)]
        public string? ModifiedBy { get; set; }

        [StringLength(450)] //Due to Primary key length
        public virtual string? TenantId { get; set; } 
    }
    public interface IBaseEntity
    {
        DateTime? DateCreated { get; set; }
        DateTime? DateModified { get; set; }
        bool? IsDeleted { get; set; }
        string? ModifiedBy { get; set; }
        string? TenantId { get; set; }
    }    
}
