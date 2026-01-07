namespace Client.Common.Services
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Windows.Storage;
    using Windows.UI.Xaml.Media.Imaging;

    /// <summary>
    /// 封面图片缓存服务实现
    /// </summary>
    public class CoverArtCacheService : ICoverArtCacheService
    {
        private const string CacheFolderName = "CoverArtCache";
        private const int DefaultExpirationDays = 7;
        
        private static readonly HttpClient HttpClient = new HttpClient();
        private StorageFolder _cacheFolder;

        /// <summary>
        /// 获取缓存的封面图片
        /// </summary>
        public async Task<BitmapImage> GetCoverArtAsync(string coverArtUrl)
        {
            if (string.IsNullOrEmpty(coverArtUrl) || coverArtUrl.StartsWith("/Assets"))
            {
                // 返回占位符图片
                return new BitmapImage(new Uri("ms-appx://" + (coverArtUrl ?? "/Assets/CoverArtPlaceholder.jpg")));
            }

            var cacheKey = GetCacheKey(coverArtUrl);
            var cacheFolder = await GetCacheFolderAsync();

            try
            {
                // 尝试从缓存获取
                var cachedFile = await cacheFolder.TryGetItemAsync(cacheKey + ".jpg") as StorageFile;
                if (cachedFile != null)
                {
                    var bitmap = new BitmapImage();
                    using (var stream = await cachedFile.OpenReadAsync())
                    {
                        await bitmap.SetSourceAsync(stream);
                    }
                    return bitmap;
                }

                // 缓存未命中，通过限流阀下载
                return await RequestThrottler.ExecuteAsync(async () =>
                {
                    var imageBytes = await HttpClient.GetByteArrayAsync(coverArtUrl);
                    
                    // 保存到缓存
                    var file = await cacheFolder.CreateFileAsync(cacheKey + ".jpg", CreationCollisionOption.ReplaceExisting);
                    await FileIO.WriteBytesAsync(file, imageBytes);

                    var bitmap = new BitmapImage();
                    using (var stream = new MemoryStream(imageBytes))
                    {
                        await bitmap.SetSourceAsync(stream.AsRandomAccessStream());
                    }
                    return bitmap;
                });
            }
            catch (Exception)
            {
                // 下载失败，返回占位符
                return new BitmapImage(new Uri("ms-appx:///Assets/CoverArtPlaceholder.jpg"));
            }
        }

        /// <summary>
        /// 获取缓存大小
        /// </summary>
        public async Task<ulong> GetCacheSizeAsync()
        {
            var cacheFolder = await GetCacheFolderAsync();
            var files = await cacheFolder.GetFilesAsync();
            
            ulong totalSize = 0;
            foreach (var file in files)
            {
                var props = await file.GetBasicPropertiesAsync();
                totalSize += props.Size;
            }
            return totalSize;
        }

        /// <summary>
        /// 清理所有缓存
        /// </summary>
        public async Task ClearAllCacheAsync()
        {
            var cacheFolder = await GetCacheFolderAsync();
            var files = await cacheFolder.GetFilesAsync();
            
            foreach (var file in files)
            {
                await file.DeleteAsync();
            }
        }

        /// <summary>
        /// 清理过期缓存
        /// </summary>
        public async Task CleanExpiredCacheAsync(int expirationDays = DefaultExpirationDays)
        {
            var cacheFolder = await GetCacheFolderAsync();
            var files = await cacheFolder.GetFilesAsync();
            var expirationDate = DateTimeOffset.Now.AddDays(-expirationDays);

            foreach (var file in files)
            {
                var props = await file.GetBasicPropertiesAsync();
                if (props.DateModified < expirationDate)
                {
                    await file.DeleteAsync();
                }
            }
        }

        /// <summary>
        /// 从 URL 提取缓存键
        /// 使用 id 和 size 参数，包含 Navidrome 的 _t 时间戳参数用于 Cache Busting
        /// </summary>
        private string GetCacheKey(string url)
        {
            var idMatch = Regex.Match(url, @"[&?]id=([^&]+)");
            var sizeMatch = Regex.Match(url, @"[&?]size=([^&]+)");
            var timestampMatch = Regex.Match(url, @"[&?]_t=([^&]+)");

            var id = idMatch.Success ? idMatch.Groups[1].Value : "unknown";
            var size = sizeMatch.Success ? sizeMatch.Groups[1].Value : "default";
            var timestamp = timestampMatch.Success ? "_" + timestampMatch.Groups[1].Value : "";

            // 清理非法文件名字符
            var key = $"{id}_{size}{timestamp}";
            return Regex.Replace(key, @"[<>:""/\\|?*]", "_");
        }

        private async Task<StorageFolder> GetCacheFolderAsync()
        {
            if (_cacheFolder == null)
            {
                _cacheFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(
                    CacheFolderName, CreationCollisionOption.OpenIfExists);
            }
            return _cacheFolder;
        }

        /// <summary>
        /// 格式化文件大小显示
        /// </summary>
        public static string FormatFileSize(ulong bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
