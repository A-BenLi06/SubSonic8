namespace Client.Common.Tests.Results
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using Client.Common.Results;
    using Client.Common.Services.DataStructures.SubsonicService;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

    [TestClass]
    public class CreatePlaylistResultTests
    {
        #region Constants

        private const string Data =
            "<subsonic-response xmlns=\"http://subsonic.org/restapi\" status=\"ok\" version=\"1.8.0\"></subsonic-response>";

        #endregion

        #region Fields

        private List<string> _songIds;

        private CreatePlaylistResultWrapper _subject;

        #endregion

        #region Public Methods and Operators

        [TestInitialize]
        public void Setup()
        {
            _songIds = new List<string>();
            _subject = new CreatePlaylistResultWrapper(new SubsonicServiceConfiguration(), "test playlist", _songIds);
        }

        [TestMethod]
        public void HandleResponse_ResponseIsEmpty_ReturnsTrue()
        {
            var result = new CreatePlaylistResultWrapper(new SubsonicServiceConfiguration(), string.Empty, new string[0]);

            result.CallHandleResponse(XDocument.Load(new StringReader(Data)));

            result.Result.Should().BeTrue();
        }

        [TestMethod]
        public void RequestUrlShouldBeCorrect()
        {
            _songIds.AddRange(new[] { "0", "1", "2", "3", "4" });

            _subject.RequestUrl.Should().EndWith("&name=test+playlist&songId=0&songId=1&songId=2&songId=3&songId=4");
        }

        [TestMethod]
        public void ViewNameShouldBeCreatePlaylist()
        {
            _subject.ResourcePath.Should().Be("createPlaylist.view");
        }

        #endregion

        internal class CreatePlaylistResultWrapper : CreatePlaylistResult
        {
            #region Constructors and Destructors

            public CreatePlaylistResultWrapper(
                ISubsonicServiceConfiguration configuration, string name, IEnumerable<string> songIds)
                : base(configuration, name, songIds)
            {
            }

            #endregion

            #region Public Methods and Operators

            public void CallHandleResponse(XDocument xDocument)
            {
                HandleResponse(xDocument);
            }

            #endregion
        }
    }
}