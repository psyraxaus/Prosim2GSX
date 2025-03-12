using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;
using System.IO;

namespace Prosim2GSX.UI.EFB.Utilities
{
    /// <summary>
    /// Provides caching functionality for frequently used resources to improve performance.
    /// </summary>
    public static class ResourceCache
    {
        private static readonly Dictionary<string, object> _cache = new();
        private static readonly object _lockObject = new();

        /// <summary>
        /// Gets a resource from the cache or creates it if it doesn't exist.
        /// </summary>
        /// <typeparam name="T">The type of resource to retrieve.</typeparam>
        /// <param name="key">The unique key for the resource.</param>
        /// <param name="factory">A function that creates the resource if it doesn't exist in the cache.</param>
        /// <returns>The cached or newly created resource.</returns>
        public static T GetOrCreate<T>(string key, Func<T> factory)
        {
            lock (_lockObject)
            {
                if (!_cache.TryGetValue(key, out var value))
                {
                    value = factory();
                    _cache[key] = value;
                }
                
                return (T)value;
            }
        }

        /// <summary>
        /// Asynchronously gets a resource from the cache or creates it if it doesn't exist.
        /// </summary>
        /// <typeparam name="T">The type of resource to retrieve.</typeparam>
        /// <param name="key">The unique key for the resource.</param>
        /// <param name="factory">A function that creates the resource if it doesn't exist in the cache.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the cached or newly created resource.</returns>
        public static async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory)
        {
            lock (_lockObject)
            {
                if (_cache.TryGetValue(key, out var value))
                {
                    return (T)value;
                }
            }
            
            var result = await factory();
            
            lock (_lockObject)
            {
                _cache[key] = result;
                return result;
            }
        }

        /// <summary>
        /// Loads an image from a file and caches it.
        /// </summary>
        /// <param name="path">The path to the image file.</param>
        /// <returns>The cached image.</returns>
        public static BitmapImage GetImage(string path)
        {
            return GetOrCreate<BitmapImage>(path, () =>
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
                image.EndInit();
                image.Freeze(); // Make it thread-safe
                return image;
            });
        }

        /// <summary>
        /// Asynchronously loads an image from a file and caches it.
        /// </summary>
        /// <param name="path">The path to the image file.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the cached image.</returns>
        public static async Task<BitmapImage> GetImageAsync(string path)
        {
            return await GetOrCreateAsync<BitmapImage>(path, async () =>
            {
                var image = new BitmapImage();
                
                await Task.Run(() =>
                {
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
                    image.EndInit();
                    image.Freeze(); // Make it thread-safe
                });
                
                return image;
            });
        }

        /// <summary>
        /// Removes a resource from the cache.
        /// </summary>
        /// <param name="key">The key of the resource to remove.</param>
        public static void Remove(string key)
        {
            lock (_lockObject)
            {
                _cache.Remove(key);
            }
        }

        /// <summary>
        /// Clears all resources from the cache.
        /// </summary>
        public static void Clear()
        {
            lock (_lockObject)
            {
                _cache.Clear();
            }
        }

        /// <summary>
        /// Gets the number of items in the cache.
        /// </summary>
        public static int Count
        {
            get
            {
                lock (_lockObject)
                {
                    return _cache.Count;
                }
            }
        }
    }
}
