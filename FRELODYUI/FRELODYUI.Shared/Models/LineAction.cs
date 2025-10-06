using FRELODYUI.Shared.Pages.Compose;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYUI.Shared.Models
{

    public enum LineAction { None, Move, Switch, Relocate }

    public class LineActionDto
    {
        public int CurrentLine { get; set; }
        public int TargetLine { get; set; }
        public bool CreateNewPart { get; set; }
        public LineAction Action { get; set; }
    }

    public class RelocateLineActionDto
    {
        public int CurrentSectionId { get; set; }
        public int CurrentLine { get; set; }
        public TabItem? TargetPart { get; set; }
        public LineAction Action { get; set; }
        public bool CreateNewPart { get; set; }
    }
}
