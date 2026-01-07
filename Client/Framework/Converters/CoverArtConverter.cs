namespace Subsonic8.Framework.Converters
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using Caliburn.Micro;
    using Client.Common.Services;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Data;
    using Windows.UI.Xaml.Media.Imaging;

    /// <summary>
    /// 将封面图片 URL 转换为 BitmapImage，通过缓存服务加载实现限流
    /// </summary>
    public class CoverArtConverter : IValueConverter
    {
        // 用于跟踪正在加载的图片，避免重复请求
        private static readonly ConcurrentDictionary<string, Task<BitmapImage>> LoadingTasks = 
            new ConcurrentDictionary<string, Task<BitmapImage>>();

        private static ICoverArtCacheService _cacheService;

        private static ICoverArtCacheService CacheService
        {
            get
            {
                if (_cacheService == null)
                {
                    try
                    {
                        _cacheService = IoC.Get<ICoverArtCacheService>();
                    }
                    catch
                    {
                        // DI 未就绪时返回 null
                    }
                }
                return _cacheService;
            }
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var url = value as string;
            
            // 空 URL 或占位符路径
            if (string.IsNullOrEmpty(url))
            {
                return new BitmapImage(new Uri("ms-appx:///Assets/CoverArtPlaceholder.jpg"));
            }

            // 本地资源路径
            if (url.StartsWith("/Assets") || url.StartsWith("ms-appx"))
            {
                return new BitmapImage(new Uri(url.StartsWith("ms-appx") ? url : "ms-appx://" + url));
            }

            // 通过缓存服务异步加载
            var bitmap = new BitmapImage();
            
            // 启动异步加载任务
            var _ = LoadImageAsync(url, bitmap);
            
            return bitmap;
        }

        private async Task LoadImageAsync(string url, BitmapImage targetBitmap)
        {
            try
            {
                if (CacheService == null)
                {
                    // 缓存服务不可用，直接设置 URI
                    targetBitmap.UriSource = new Uri(url);
                    return;
                }

                // 使用缓存服务加载（内部已集成限流）
                var cachedBitmap = await CacheService.GetCoverArtAsync(url);
                
                // 由于 BitmapImage 创建后无法直接复制，需要在 UI 线程更新
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    Windows.UI.Core.CoreDispatcherPriority.Normal,
                    () =>
                    {
                        try
                        {
                            // 直接设置 URI，因为缓存服务已经下载完成
                            targetBitmap.UriSource = new Uri(url);
                        }
                        catch
                        {
                            // 设置失败时使用占位符
                        }
                    });
            }
            catch
            {
                // 加载失败，使用占位符
                try
                {
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        Windows.UI.Core.CoreDispatcherPriority.Normal,
                        () =>
                        {
                            targetBitmap.UriSource = new Uri("ms-appx:///Assets/CoverArtPlaceholder.jpg");
                        });
                }
                catch { }
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
