namespace Common.Mocks
{
    using System;
    using Client.Common.Models.Subsonic;
    using Client.Common.Services;
    using Client.Common.Services.DataStructures.SubsonicService;
    using Common.Mocks.Results;

    public class MockSubsonicService : SubsonicService
    {
        #region Fields

        private bool _hasValidSubsonicUrl;

        #endregion

        #region Public Properties

        public int GetCoverArtForIdCallCount { get; set; }

        public int GetUriForFileWithIdCallCount { get; set; }

        public int GetUriForVideoWithIdCallCount { get; set; }

        public override bool HasValidSubsonicUrl
        {
            get
            {
                return _hasValidSubsonicUrl;
            }
        }

        #endregion

        #region Constructors and Destructors

        public MockSubsonicService()
        {
            GetSong = id => new MockGetSongResult(id);
            Search = s => new MockSearchResult { GetResultFunc = () => new SearchResultCollection() };
        }

        #endregion

        #region Public Methods and Operators

        public override string GetCoverArtForId(string coverArt, ImageType imageType)
        {
            GetCoverArtForIdCallCount++;

            return "http://test.mock";
        }

        public override Uri GetUriForFileWithId(string id)
        {
            GetUriForFileWithIdCallCount++;

            return new Uri(string.Format("http://subsonic.org?id={0}", id));
        }

        public override Uri GetUriForFileWithId(string id, bool transcodeToMp3)
        {
            GetUriForFileWithIdCallCount++;

            var url = string.Format("http://subsonic.org?id={0}", id);
            if (transcodeToMp3)
            {
                url += "&format=mp3&maxBitRate=320";
            }

            return new Uri(url);
        }

        public override Uri GetUriForVideoWithId(string id, int timeOffset = 0, int maxBitrate = 0)
        {
            GetUriForVideoWithIdCallCount++;

            return new Uri("http://test.mock");
        }

        public void SetHasValidSubsonicUrl(bool value)
        {
            _hasValidSubsonicUrl = value;
        }

        #endregion
    }
}