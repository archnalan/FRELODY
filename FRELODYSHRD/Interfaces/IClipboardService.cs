using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYUI.Services
{
    public interface IClipboardService
    {
        Task CopyToClipboardAsync(string text);
        Task<bool> IsClipboardAvailableAsync();
    }
}
