namespace Common.Mocks.Results
{
    using Client.Common.Models.Subsonic;
    using Client.Common.Results;
    using global::Common.Mocks;

    public class MockGetSongResult : MockServiceResultBase<Song>, IGetSongResult
    {
        #region Constructors and Destructors

        public MockGetSongResult(string id = null)
        {
            GetResultFunc = () => new Song { Id = id ?? string.Empty };
        }

        #endregion
    }
}