using FRELODYAPIs.Areas.Admin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYUI.Shared.Models
{
    public class CategoryView
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool IsExpanded { get; set; }
        public List<SongResult> Songs { get; set; } = new();
    }

}
