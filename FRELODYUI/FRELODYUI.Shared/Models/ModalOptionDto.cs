using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYUI.Shared.Models
{
    public class ModalOptionDto
    {
        public string? Title { get; set; }
        public string? SubTitle { get; set; }
        public string? Message { get; set; }
        public string? ButtonText { get; set; }
        public string? DangerText { get; set; }
        public string? HyperlinkText { get; set; }
        public OptionType OptionType { get; set; }
        public ModalContext? Context { get; set; } 
        public bool? HideActionConfirmBtn { get; set; }
    }
    public enum OptionType
    {
        Success = 1,
        Warning = 2,
        Info = 3,
        Error = 4,
        Confirmation = 5,
        Feedback = 6
    }
    public class ModalContext
    {
        public string ActionType { get; set; } = "";
        public object Data { get; set; } = null!;
    }
}
