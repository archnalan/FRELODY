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

    }
}
