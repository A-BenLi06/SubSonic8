namespace Client.Common.Results
{
    using System.Collections.Generic;

    public interface IUpdatePlaylistResult : IEmptyResponseResult
    {
        #region Public Properties

        string Id { get; }

        IEnumerable<string> SongIdsToAdd { get; }

        IEnumerable<int> SongIndexesToRemove { get; }

        #endregion
    }
}