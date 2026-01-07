namespace Client.Common.Services
{
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Windows.Storage;
    using Windows.UI.Xaml.Media.Imaging;

    /// <summary>
    /// 封面图片缓存服务接口
    /// </summary>
    public interface ICoverArtCacheService
    {
        /// <summary>
        /// 获取缓存的封面图片，如果不存在则下载并缓存
        /// </summary>
        Task<BitmapImage> GetCoverArtAsync(string coverArtUrl);

        /// <summary>
        /// 获取缓存大小（字节）
        /// </summary>
        Task<ulong> GetCacheSizeAsync();

        /// <summary>
        /// 清理所有缓存
        /// </summary>
        Task ClearAllCacheAsync();

        /// <summary>
        /// 清理过期缓存（超过指定天数）
        /// </summary>
        Task CleanExpiredCacheAsync(int expirationDays = 7);
    }
}
