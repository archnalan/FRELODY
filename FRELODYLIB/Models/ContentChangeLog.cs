using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using FRELODYAPP.Models.SubModels;
using FRELODYSHRD.Constants;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYLIB.Models
{
    [Index(nameof(ChangedByUserId), nameof(EntityId), IsUnique = true)]
    public class ContentChangeLog : BaseEntity
    {
        public EntityLogType EntityType { get; set; } 
        public string EntityId { get; set; } = string.Empty;
        public ChangeLogType ChangeType { get; set; } 
        public string? ChangedByUserId { get; set; }
        public DateTimeOffset ChangeTime { get; set; }
        public string? ChangeDetails { get; set; } // JSON serialized changes
        public bool IsPublicContent { get; set; }

        [ForeignKey(nameof(ChangedByUserId))]
        public virtual User? ChangedByUser { get; set; }
    }
}
