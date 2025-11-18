using FRELODYAPP.Models.SubModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYLIB.Models
{
    [Index(nameof(UserId), nameof(LoginTime),IsUnique = true)]
    public class UserLoginHistory : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public string? DeviceInfo { get; set; }
        public string? IpAddress { get; set; }
        public string? Location { get; set; }
        public DateTimeOffset LoginTime { get; set; }
        public DateTimeOffset? LastLogoutTime { get; set; }
        public bool IsActiveSession { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }
    }
}
