namespace Subsonic8.Phone.Services
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices.WindowsRuntime;
    using System.Threading.Tasks;
    using Windows.Web.Http;

    /// <summary>
    /// WinRT-based HttpClient wrapper for Windows Phone 8.1.
    /// Uses Windows.Web.Http.HttpClient which works better on WP8.1 than System.Net.Http.
    /// </summary>
    public class WinRTHttpClient : IDisposable
    {
        private readonly HttpClient _client;

        public WinRTHttpClient()
        {
            _client = new HttpClient();
        }

        /// <summary>
        /// Performs a GET request and returns the response as a stream.
        /// </summary>
        public async Task<Stream> GetStreamAsync(string url)
        {
            var uri = new Uri(url);
            var response = await _client.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            
            var buffer = await response.Content.ReadAsBufferAsync();
            return buffer.AsStream();
        }

        /// <summary>
        /// Performs a GET request and returns the response as a string.
        /// </summary>
        public async Task<string> GetStringAsync(string url)
        {
            var uri = new Uri(url);
            var response = await _client.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Performs a GET request and returns the response as a byte array.
        /// </summary>
        public async Task<byte[]> GetBytesAsync(string url)
        {
            var uri = new Uri(url);
            var response = await _client.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            
            var buffer = await response.Content.ReadAsBufferAsync();
            return buffer.ToArray();
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
