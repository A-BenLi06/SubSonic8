namespace Client.Common.Results
{
    using System.Collections.Generic;

    public interface ICreatePlaylistResult : IEmptyResponseResult
    {
        #region Public Properties

        string Name { get; }

        IEnumerable<string> SongIds { get; }

        #endregion
    }
}