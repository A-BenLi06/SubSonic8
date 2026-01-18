namespace Client.Common.Services
{
    using System;
    using System.Collections.Generic;
    using Client.Common.Results;
    using Client.Common.Services.DataStructures.SubsonicService;

    public interface ISubsonicService
    {
        #region Public Properties

        bool IsVideoPlaybackInitialized { get; set; }

        SubsonicServiceConfiguration Configuration { get; set; }

        Func<string, IEnumerable<string>, ICreatePlaylistResult> CreatePlaylist { get; set; }

        Func<string, IDeletePlaylistResult> DeletePlaylist { get; set; }

        Func<string, IGetAlbumResult> GetAlbum { get; set; }

        Func<IGetAllPlaylistsResult> GetAllPlaylists { get; set; }

        Func<string, IGetArtistResult> GetArtist { get; set; }

        Func<string, IGetIndexResult> GetIndex { get; set; }

        Func<string, IGetMusicDirectoryResult> GetMusicDirectory { get; set; }

        Func<IGetRootResult> GetMusicFolders { get; set; }

        Func<string, IGetPlaylistResult> GetPlaylist { get; set; }

        Func<string, IGetSongResult> GetSong { get; set; }

        bool HasValidSubsonicUrl { get; }

        Func<IPingResult> Ping { get; set; }

        Func<string, string, IRenamePlaylistResult> RenamePlaylist { get; set; }

        Func<string, ISearchResult> Search { get; set; }

        Func<string, IEnumerable<string>, IEnumerable<int>, IUpdatePlaylistResult> UpdatePlaylist { get; set; }

        Func<int, IGetRandomSongsResult> GetRandomSongs { get; set; }

        Func<string, IGetAlbumListResult> GetAlbumList { get; set; }

        #endregion

        #region Public Methods and Operators

        string GetCoverArtForId(string coverArt);

        string GetCoverArtForId(string coverArt, ImageType imageType);

        Uri GetUriForFileWithId(string id);

        Uri GetUriForFileWithId(string id, bool transcodeToMp3);

        Uri GetUriForVideoStartingAt(Uri source, double totalSeconds);

        Uri GetUriForVideoWithId(string id, int timeOffset = 0, int maxBitRate = 0);

        #endregion
    }
}