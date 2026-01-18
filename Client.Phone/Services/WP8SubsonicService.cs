namespace Subsonic8.Phone.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Client.Common.Models;
    using Client.Common.Models.Subsonic;

    /// <summary>
    /// WP8.1-specific Subsonic service implementation using WinRT HttpClient.
    /// </summary>
    public class WP8SubsonicService : IDisposable
    {
        private readonly WinRTHttpClient _httpClient;
        private string _baseUrl;
        private string _username;
        private string _password;
        private bool _compatibleMode;

        public WP8SubsonicService()
        {
            _httpClient = new WinRTHttpClient();
        }

        public void Configure(string baseUrl, string username, string password)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _username = username;
            _password = password;
        }

        /// <summary>
        /// Sets the compatible mode (transcode lossless to MP3 320kbps).
        /// </summary>
        public void SetCompatibleMode(bool enabled)
        {
            _compatibleMode = enabled;
        }

        public bool CompatibleMode
        {
            get { return _compatibleMode; }
            set { _compatibleMode = value; }
        }

        private string BuildApiUrl(string method, Dictionary<string, string> parameters = null)
        {
            var url = $"{_baseUrl}/rest/{method}?u={Uri.EscapeDataString(_username)}&p={Uri.EscapeDataString(_password)}&v=1.16.1&c=Subsonic8WP&f=xml";
            
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    url += $"&{param.Key}={Uri.EscapeDataString(param.Value)}";
                }
            }
            
            return url;
        }

        public async Task<bool> PingAsync()
        {
            try
            {
                var url = BuildApiUrl("ping");
                var responseXml = await _httpClient.GetStringAsync(url);
                var doc = XDocument.Parse(responseXml);
                var root = doc.Root;
                
                if (root == null) return false;
                
                var ns = root.Name.Namespace;
                var status = root.Attribute("status")?.Value;
                return status == "ok";
            }
            catch
            {
                return false;
            }
        }

        public async Task<Song> GetSongAsync(string id)
        {
            var url = BuildApiUrl("getSong", new Dictionary<string, string> { { "id", id } });
            var responseXml = await _httpClient.GetStringAsync(url);
            var doc = XDocument.Parse(responseXml);
            var root = doc.Root;
            
            if (root == null) return null;
            
            var ns = root.Name.Namespace;
            var songElement = root.Element(ns + "song");
            
            if (songElement == null) return null;
            
            return new Song
            {
                Id = songElement.Attribute("id")?.Value,
                Title = songElement.Attribute("title")?.Value,
                Artist = songElement.Attribute("artist")?.Value,
                Album = songElement.Attribute("album")?.Value,
                CoverArt = songElement.Attribute("coverArt")?.Value,
                Duration = ParseInt(songElement.Attribute("duration")?.Value)
            };
        }

        public Uri GetStreamUri(string songId)
        {
            var parameters = new Dictionary<string, string> { { "id", songId } };
            
            // When compatible mode is enabled, transcode to MP3 320kbps
            if (_compatibleMode)
            {
                parameters.Add("format", "mp3");
                parameters.Add("maxBitRate", "320");
            }
            
            var url = BuildApiUrl("stream", parameters);
            return new Uri(url);
        }

        public string GetCoverArtUrl(string coverArtId, int size = 300)
        {
            return BuildApiUrl("getCoverArt", new Dictionary<string, string>
            {
                { "id", coverArtId },
                { "size", size.ToString() }
            });
        }

        public async Task<List<Album>> GetAlbumsAsync(string artistId)
        {
            var albums = new List<Album>();
            var url = BuildApiUrl("getArtist", new Dictionary<string, string> { { "id", artistId } });
            
            try
            {
                var responseXml = await _httpClient.GetStringAsync(url);
                var doc = XDocument.Parse(responseXml);
                var root = doc.Root;
                
                if (root == null) return albums;
                
                var ns = root.Name.Namespace;
                var artistElement = root.Element(ns + "artist");
                
                if (artistElement == null) return albums;
                
                foreach (var albumElement in artistElement.Elements(ns + "album"))
                {
                    albums.Add(new Album
                    {
                        Id = albumElement.Attribute("id")?.Value,
                        Name = albumElement.Attribute("name")?.Value,
                        Artist = albumElement.Attribute("artist")?.Value,
                        CoverArt = albumElement.Attribute("coverArt")?.Value
                    });
                }
            }
            catch
            {
                // Return empty list on error
            }
            
            return albums;
        }

        public async Task<List<Song>> GetAlbumSongsAsync(string albumId)
        {
            var songs = new List<Song>();
            var url = BuildApiUrl("getAlbum", new Dictionary<string, string> { { "id", albumId } });
            
            try
            {
                var responseXml = await _httpClient.GetStringAsync(url);
                var doc = XDocument.Parse(responseXml);
                var root = doc.Root;
                
                if (root == null) return songs;
                
                var ns = root.Name.Namespace;
                var albumElement = root.Element(ns + "album");
                
                if (albumElement == null) return songs;
                
                foreach (var songElement in albumElement.Elements(ns + "song"))
                {
                    songs.Add(new Song
                    {
                        Id = songElement.Attribute("id")?.Value,
                        Title = songElement.Attribute("title")?.Value,
                        Artist = songElement.Attribute("artist")?.Value,
                        Album = songElement.Attribute("album")?.Value,
                        CoverArt = songElement.Attribute("coverArt")?.Value,
                        Duration = ParseInt(songElement.Attribute("duration")?.Value)
                    });
                }
            }
            catch
            {
                // Return empty list on error
            }
            
            return songs;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        private static int ParseInt(string value)
        {
            int result;
            return int.TryParse(value, out result) ? result : 0;
        }
    }
}
