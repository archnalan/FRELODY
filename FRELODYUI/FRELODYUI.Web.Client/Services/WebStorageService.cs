using Blazored.LocalStorage;
using FRELODYUI.Shared.Services;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace FRELODYUI.Web.Client.Services
{
    public class WebStorageService : IStorageService
    {
        private readonly ILocalStorageService _localStorage;
        private readonly ISyncLocalStorageService _syncLocalStorage;
        private readonly IFormFactor _formFactor;
        private readonly IJSRuntime _jsRuntime;
        private readonly ILogger<WebStorageService> _logger;

        public WebStorageService(
            ILocalStorageService localStorage,
            ISyncLocalStorageService syncLocalStorage,
            IFormFactor formFactor,
            IJSRuntime jsRuntime,
            ILogger<WebStorageService> logger)
        {
            _localStorage = localStorage;
            _syncLocalStorage = syncLocalStorage;
            _formFactor = formFactor;
            _jsRuntime = jsRuntime;
            _logger = logger;
        }

        public event Action? OnTokenRemoved;

        public async Task<T> GetItemAsync<T>(string objectKey)
        {
            try
            {
                if (string.IsNullOrEmpty(objectKey))
                {
                    _logger.LogWarning("Object key cannot be null or empty");
                    return default(T);
                }

                var formFactor = GetFormFactorSafely();
                
                if (IsWebClient(formFactor))
                {
                    var result = await _localStorage.GetItemAsync<T>(objectKey);
                    _logger.LogDebug("Successfully retrieved item for key: {ObjectKey} using async storage", objectKey);
                    return result;
                }
                else
                {
                    // For non-web clients, try sync storage if available
                    if (IsInProcessRuntime())
                    {
                        var result = _syncLocalStorage.GetItem<T>(objectKey);
                        _logger.LogDebug("Successfully retrieved item for key: {ObjectKey} using sync storage", objectKey);
                        return result;
                    }
                    else
                    {
                        // Fall back to async if sync is not supported
                        _logger.LogWarning("Sync runtime not available, falling back to async for key: {ObjectKey}", objectKey);
                        var result = await _localStorage.GetItemAsync<T>(objectKey);
                        return result;
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation while retrieving item for key: {ObjectKey}", objectKey);
                return default(T);
            }
            catch (JSException ex)
            {
                _logger.LogError(ex, "JavaScript error while retrieving item for key: {ObjectKey}", objectKey);
                return default(T);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving item for key: {ObjectKey}", objectKey);
                return default(T);
            }
        }

        public async Task RemoveItemAsync(string objectKey)
        {
            try
            {
                if (string.IsNullOrEmpty(objectKey))
                {
                    _logger.LogWarning("Object key cannot be null or empty");
                    return;
                }

                var formFactor = GetFormFactorSafely();
                bool itemRemoved = false;

                if (IsWebClient(formFactor))
                {
                    await _localStorage.RemoveItemAsync(objectKey);
                    itemRemoved = true;
                    _logger.LogDebug("Successfully removed item for key: {ObjectKey} using async storage", objectKey);
                }
                else
                {
                    if (IsInProcessRuntime())
                    {
                        _syncLocalStorage.RemoveItem(objectKey);
                        itemRemoved = true;
                        _logger.LogDebug("Successfully removed item for key: {ObjectKey} using sync storage", objectKey);
                    }
                    else
                    {
                        // Fall back to async if sync is not supported
                        _logger.LogWarning("Sync runtime not available, falling back to async for key: {ObjectKey}", objectKey);
                        await _localStorage.RemoveItemAsync(objectKey);
                        itemRemoved = true;
                    }
                }

                // Trigger token removal event if successful and it's a token-related key
                if (itemRemoved && IsTokenRelatedKey(objectKey))
                {
                    OnTokenRemoved?.Invoke();
                    _logger.LogInformation("Token removal event triggered for key: {ObjectKey}", objectKey);
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation while removing item for key: {ObjectKey}", objectKey);
                throw;
            }
            catch (JSException ex)
            {
                _logger.LogError(ex, "JavaScript error while removing item for key: {ObjectKey}", objectKey);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item for key: {ObjectKey}", objectKey);
                throw;
            }
        }

        public async Task SetItemAsync<T>(string objectKey, T objectValue)
        {
            try
            {
                if (string.IsNullOrEmpty(objectKey))
                {
                    _logger.LogWarning("Object key cannot be null or empty");
                    return;
                }

                if (objectValue == null)
                {
                    _logger.LogWarning("Cannot store null value for key: {ObjectKey}", objectKey);
                    return;
                }

                var formFactor = GetFormFactorSafely();

                if (IsWebClient(formFactor))
                {
                    await _localStorage.SetItemAsync(objectKey, objectValue);
                    _logger.LogDebug("Successfully set item for key: {ObjectKey} using async storage", objectKey);
                }
                else
                {
                    // Check if IJSInProcessRuntime is available before calling sync method
                    if (IsInProcessRuntime())
                    {
                        _syncLocalStorage.SetItem(objectKey, objectValue);
                        _logger.LogDebug("Successfully set item for key: {ObjectKey} using sync storage", objectKey);
                    }
                    else
                    {
                        // Fall back to async if sync is not supported
                        _logger.LogWarning("Sync runtime not available, falling back to async for key: {ObjectKey}", objectKey);
                        await _localStorage.SetItemAsync(objectKey, objectValue);
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation while setting item for key: {ObjectKey}", objectKey);
                throw;
            }
            catch (JSException ex)
            {
                _logger.LogError(ex, "JavaScript error while setting item for key: {ObjectKey}", objectKey);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting item for key: {ObjectKey}", objectKey);
                throw;
            }
        }

        #region Helper Methods

        private string GetFormFactorSafely()
        {
            try
            {
                return _formFactor.GetFormFactor();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting form factor, defaulting to 'webClient'");
                return "webClient";
            }
        }

        private bool IsWebClient(string formFactor)
        {
            return string.Equals(formFactor, "webClient", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsInProcessRuntime()
        {
            return _jsRuntime is IJSInProcessRuntime;
        }

        private bool IsTokenRelatedKey(string objectKey)
        {
            return objectKey.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                   objectKey.Contains("session", StringComparison.OrdinalIgnoreCase) ||
                   objectKey.Contains("auth", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if a key exists in storage
        /// </summary>
        /// <param name="objectKey">The key to check</param>
        /// <returns>True if the key exists, false otherwise</returns>
        public async Task<bool> ContainsKeyAsync(string objectKey)
        {
            try
            {
                if (string.IsNullOrEmpty(objectKey))
                {
                    return false;
                }

                var formFactor = GetFormFactorSafely();

                if (IsWebClient(formFactor))
                {
                    return await _localStorage.ContainKeyAsync(objectKey);
                }
                else
                {
                    if (IsInProcessRuntime())
                    {
                        return _syncLocalStorage.ContainKey(objectKey);
                    }
                    else
                    {
                        return await _localStorage.ContainKeyAsync(objectKey);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if key exists: {ObjectKey}", objectKey);
                return false;
            }
        }

        /// <summary>
        /// Clears all items from storage. Use with caution.
        /// </summary>
        public async Task ClearAsync()
        {
            try
            {
                var formFactor = GetFormFactorSafely();

                if (IsWebClient(formFactor))
                {
                    await _localStorage.ClearAsync();
                }
                else
                {
                    if (IsInProcessRuntime())
                    {
                        _syncLocalStorage.Clear();
                    }
                    else
                    {
                        await _localStorage.ClearAsync();
                    }
                }

                _logger.LogInformation("All storage cleared");
                OnTokenRemoved?.Invoke();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing storage");
                throw;
            }
        }

        /// <summary>
        /// Gets the number of items in storage
        /// </summary>
        /// <returns>The number of items stored</returns>
        public async Task<int> LengthAsync()
        {
            try
            {
                var formFactor = GetFormFactorSafely();

                if (IsWebClient(formFactor))
                {
                    return await _localStorage.LengthAsync();
                }
                else
                {
                    if (IsInProcessRuntime())
                    {
                        return _syncLocalStorage.Length();
                    }
                    else
                    {
                        return await _localStorage.LengthAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting storage length");
                return 0;
            }
        }

        #endregion
    }
}
