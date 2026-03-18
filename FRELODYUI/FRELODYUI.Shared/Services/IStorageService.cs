using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYUI.Shared.Services
{
    public interface IStorageService
    {
        event Action? OnTokenRemoved;

        Task SetItemAsync<T>(string objectKey, T objectValue);
        Task RemoveItemAsync(string objectKey);
        Task<T> GetItemAsync<T>(string objectKey);

        /// <summary>
        /// Returns false when JS interop is unavailable (e.g. during server-side prerendering).
        /// Use this in OnAfterRenderAsync to gate session-recovery reads.
        /// </summary>
        Task<bool> IsStorageAvailableAsync();
    }
}
