namespace Client.Common.Results
{
    public interface IDeletePlaylistResult : IEmptyResponseResult
    {
        #region Public Properties

        string Id { get; }

        #endregion
    }
}