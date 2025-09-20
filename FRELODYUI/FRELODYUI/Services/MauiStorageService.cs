using FRELODYUI.Shared.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FRELODYUI.Services
{
    public class MauiStorageService : IStorageService
    {
        private readonly ILogger<MauiStorageService> _logger;

        public MauiStorageService(ILogger<MauiStorageService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

                string storedValue = Preferences.Get(objectKey, null);
                
                if (string.IsNullOrEmpty(storedValue))
                {
                    _logger.LogDebug("No value found for key: {ObjectKey}", objectKey);
                    return default(T);
                }

                // Handle different types appropriately
                if (typeof(T) == typeof(string))
                {
                    return (T)(object)storedValue;
                }

                // For complex objects, deserialize from JSON
                var deserializedValue = JsonSerializer.Deserialize<T>(storedValue);
                _logger.LogDebug("Successfully retrieved and deserialized value for key: {ObjectKey}", objectKey);
                
                return deserializedValue;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize stored value for key: {ObjectKey}", objectKey);
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

                // Check if the key exists before removing
                if (Preferences.ContainsKey(objectKey))
                {
                    Preferences.Remove(objectKey);
                    _logger.LogDebug("Successfully removed item for key: {ObjectKey}", objectKey);

                    // Notify subscribers if this is a token removal
                    if (objectKey.Contains("token", StringComparison.OrdinalIgnoreCase) || 
                        objectKey.Contains("session", StringComparison.OrdinalIgnoreCase))
                    {
                        OnTokenRemoved?.Invoke();
                        _logger.LogInformation("Token removal event triggered for key: {ObjectKey}", objectKey);
                    }
                }
                else
                {
                    _logger.LogDebug("Key not found for removal: {ObjectKey}", objectKey);
                }
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

                string valueToStore;

                // Handle string values directly
                if (typeof(T) == typeof(string))
                {
                    valueToStore = objectValue.ToString();
                }
                else
                {
                    // Serialize complex objects to JSON
                    valueToStore = JsonSerializer.Serialize(objectValue);
                }

                Preferences.Set(objectKey, valueToStore);
                _logger.LogDebug("Successfully stored value for key: {ObjectKey}", objectKey);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to serialize value for key: {ObjectKey}", objectKey);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing item for key: {ObjectKey}", objectKey);
                throw;
            }
        }

        /// <summary>
        /// Clears all stored preferences. Use with caution.
        /// </summary>
        public async Task ClearAllAsync()
        {
            try
            {
                Preferences.Clear();
                _logger.LogInformation("All preferences cleared");
                OnTokenRemoved?.Invoke();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing all preferences");
                throw;
            }
        }

        /// <summary>
        /// Checks if a key exists in storage
        /// </summary>
        /// <param name="objectKey">The key to check</param>
        /// <returns>True if the key exists, false otherwise</returns>
        public bool ContainsKey(string objectKey)
        {
            try
            {
                if (string.IsNullOrEmpty(objectKey))
                {
                    return false;
                }

                return Preferences.ContainsKey(objectKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if key exists: {ObjectKey}", objectKey);
                return false;
            }
        }
    }
}
