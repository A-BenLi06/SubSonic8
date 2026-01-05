namespace Client.Tests.Mocks
{
    using System.Collections.Generic;
    using Client.Common.Results;
    using global::Common.Mocks;

    public class MockUpdatePlaylistResult : MockServiceResultBase<bool>, IUpdatePlaylistResult
    {
        #region Public Properties

        public string Id { get; private set; }

        public IEnumerable<string> SongIdsToAdd { get; private set; }

        public IEnumerable<int> SongIndexesToRemove { get; private set; }

        #endregion
    }
}