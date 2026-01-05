namespace Client.Common.Results
{
    using Client.Common.Models.Subsonic;

    public interface IGetPlaylistResult : IServiceResultBase<Playlist>
    {
        #region Public Properties

        string Id { get; }

        #endregion
    }
}