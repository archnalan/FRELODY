using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FRELODYAPP.Models.SubModels
{
    public class UserRefreshToken : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public string Token { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }

        public DateTime? RevokedDate { get; set; }

        public string? ReplacedByToken { get; set; }

        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}