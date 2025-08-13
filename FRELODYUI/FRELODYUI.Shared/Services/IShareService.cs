using FRELODYSHRD.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYUI.Services
{
    public interface IShareService
    {
        Task<ShareLinkDto?> GenerateShareLinkAsync(string songId);
        Task<string> GetShareUrlAsync(string shareToken);
        Task NotifyLinkCopiedAsync();
    }
}
