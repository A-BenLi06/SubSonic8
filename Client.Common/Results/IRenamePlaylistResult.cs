namespace Client.Common.Results
{
    public interface IRenamePlaylistResult : IServiceResultBase<bool>
    {
        #region Public Properties

        string Id { get; }

        string Name { get; }

        #endregion
    }
}