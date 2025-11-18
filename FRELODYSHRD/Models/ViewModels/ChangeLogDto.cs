using FRELODYSHRD.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Models.ViewModels
{

    public class ChangeLogDto
    {
        public EntityLogType EntityType { get; set; }
        public string EntityId { get; set; } = string.Empty;
        public ChangeLogType ChangeType { get; set; }
        public string? ChangedByUserId { get; set; }
        public object? ChangeDetails { get; set; }
    }
}
