namespace Client.Common.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text.RegularExpressions;
    using Caliburn.Micro;
    using Client.Common.Results;
    using Client.Common.Services.DataStructures.SubsonicService;

    public class SubsonicService : PropertyChangedBase, ISubsonicService
    {
        #region Constants

        public const string CoverArtPlaceholder = @"/Assets/CoverArtPlaceholder.jpg";

        #endregion

        #region Fields

        private SubsonicServiceConfiguration _configuration;

        #endregion

        #region Constructors and Destructors

        public SubsonicService()
        {
            GetMusicFolders = GetMusicFoldersImpl;
            GetMusicDirectory = GetMusicDirectoryImpl;
            GetAlbum = GetAlbumImpl;
            GetArtist = GetArtistImpl;
            GetSong = GetSongImpl;
            GetIndex = GetIndexImpl;
            Search = SearchImpl;
            GetAllPlaylists = GetAllPlaylistsImpl;
            GetPlaylist = GetPlaylistImpl;
            DeletePlaylist = DeletePlaylistImpl;
            CreatePlaylist = CreatePlaylistImpl;
            UpdatePlaylist = UpdatePlaylistResultImpl;
            RenamePlaylist = RenamePlaylistImpl;
            Ping = PingImpl;
            GetRandomSongs = GetRandomSongsImpl;
        }

        #endregion

        #region Public Properties

        public SubsonicServiceConfiguration Configuration
        {
            get
            {
                return _configuration;
            }

            set
            {
                _configuration = value;
                NotifyOfPropertyChange();
            }
        }

        public Func<string, IEnumerable<string>, ICreatePlaylistResult> CreatePlaylist { get; set; }

        public Func<string, IDeletePlaylistResult> DeletePlaylist { get; set; }

        public Func<string, IGetAlbumResult> GetAlbum { get; set; }

        public Func<IGetAllPlaylistsResult> GetAllPlaylists { get; set; }

        public Func<string, IGetArtistResult> GetArtist { get; set; }

        public Func<string, IGetIndexResult> GetIndex { get; set; }

        public Func<string, IGetMusicDirectoryResult> GetMusicDirectory { get; set; }

        public Func<IGetRootResult> GetMusicFolders { get; set; }

        public Func<string, IGetPlaylistResult> GetPlaylist { get; set; }

        public Func<string, IGetSongResult> GetSong { get; set; }

        public virtual bool HasValidSubsonicUrl
        {
            get
            {
                return Configuration != null && !string.IsNullOrEmpty(Configuration.BaseUrl);
            }
        }

        public Func<IPingResult> Ping { get; set; }

        public Func<string, string, IRenamePlaylistResult> RenamePlaylist { get; set; }

        public Func<string, ISearchResult> Search { get; set; }

        public Func<string, IEnumerable<string>, IEnumerable<int>, IUpdatePlaylistResult> UpdatePlaylist { get; set; }

        public Func<int, IGetRandomSongsResult> GetRandomSongs { get; set; }

        public bool IsVideoPlaybackInitialized { get; set; }

        #endregion

        #region Public Methods and Operators

        public string GetCoverArtForId(string coverArt)
        {
            return GetCoverArtForId(coverArt, ImageType.Thumbnail);
        }

        public virtual string GetCoverArtForId(string coverArt, ImageType imageType)
        {
            string result;
            if (!string.IsNullOrEmpty(coverArt))
            {
                result = string.Format(
                    _configuration.RequestFormatWithUsernameAndPassword(),
                    "getCoverArt.view",
                    _configuration.Username,
                    _configuration.EncodedPassword) + string.Format("&id={0}", coverArt)
                         + string.Format("&size={0}", (int)imageType);
            }
            else
            {
                result = CoverArtPlaceholder;
            }

            return result;
        }

        public virtual Uri GetUriForFileWithId(string id)
        {
            return
                new Uri(
                    string.Format(
                        _configuration.RequestFormatWithUsernameAndPassword(),
                        "stream.view",
                        _configuration.Username,
                        _configuration.EncodedPassword) + string.Format("&id={0}", id));
        }

        public Uri GetUriForVideoStartingAt(Uri source, double totalSeconds)
        {
            var uriString = source.ToString();
            var regex = new Regex("(.*)(?<TIMEOFFSET>&timeOffset=)([0-9]{1,})(.*)");

            var regeExFormat = string.Format("$1${{TIMEOFFSET}}{0}$3", Math.Floor(totalSeconds));

            return new Uri(regex.Replace(uriString, regeExFormat));
        }

        public virtual Uri GetUriForVideoWithId(string id, int timeOffset = 0, int maxBitRate = 0)
        {
            var uriString = string.Format(
                "{0}stream/stream.ts?id={1}&hls=true&timeOffset={2}", _configuration.BaseUrl, id, timeOffset);
            if (maxBitRate > 0)
            {
                uriString += string.Format("&maxBitRate={0}", maxBitRate);
            }

            return new Uri(uriString);
        }

        #endregion

        #region Methods

        private ICreatePlaylistResult CreatePlaylistImpl(string name, IEnumerable<string> songIds)
        {
            return new CreatePlaylistResult(Configuration, name, songIds);
        }

        private IDeletePlaylistResult DeletePlaylistImpl(string id)
        {
            return new DeletePlaylistResult(Configuration, id);
        }

        private IGetAlbumResult GetAlbumImpl(string id)
        {
            return new GetAlbumResult(_configuration, id);
        }

        private IGetAllPlaylistsResult GetAllPlaylistsImpl()
        {
            return new GetAllPlaylistsResult(_configuration);
        }

        private IGetArtistResult GetArtistImpl(string id)
        {
            return new GetArtistsResult(_configuration, id);
        }

        private IGetIndexResult GetIndexImpl(string musicFolderId)
        {
            return new GetIndexResult(_configuration, musicFolderId);
        }

        private IGetMusicDirectoryResult GetMusicDirectoryImpl(string id)
        {
            return new GetMusicDirectoryResult(_configuration, id);
        }

        private IGetRootResult GetMusicFoldersImpl()
        {
            return new GetRootResult(_configuration);
        }

        private IGetPlaylistResult GetPlaylistImpl(string id)
        {
            return new GetPlaylistResult(_configuration, id);
        }

        private IGetSongResult GetSongImpl(string id)
        {
            return new GetSongResult(_configuration, id);
        }

        private PingResult PingImpl()
        {
            return new PingResult(Configuration);
        }

        private IRenamePlaylistResult RenamePlaylistImpl(string id, string name)
        {
            return new RenamePlaylistResult(Configuration, id, name);
        }

        private ISearchResult SearchImpl(string query)
        {
            return new SearchResult(_configuration, query);
        }

        private IUpdatePlaylistResult UpdatePlaylistResultImpl(
            string id, IEnumerable<string> songIdsToAdd, IEnumerable<int> songIndexesToRemove)
        {
            return new UpdatePlaylistResult(Configuration, id, songIdsToAdd, songIndexesToRemove);
        }

        private IGetRandomSongsResult GetRandomSongsImpl(int numberOfSongs)
        {
            return new GetRandomSongsResult(Configuration, numberOfSongs);
        }

        #endregion
    }
}