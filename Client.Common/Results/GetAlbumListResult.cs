namespace Client.Common.Results
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using System.Xml.Serialization;
    using Client.Common.Models.Subsonic;
    using Client.Common.Services.DataStructures.SubsonicService;

    public class GetAlbumListResult : ServiceResultBase<IList<Album>>, IGetAlbumListResult
    {
        #region Constructors and Destructors

        public GetAlbumListResult(ISubsonicServiceConfiguration configuration, string musicFolderId)
            : base(configuration)
        {
            MusicFolderId = musicFolderId;
        }

        #endregion

        #region Public Properties

        public string MusicFolderId { get; private set; }

        public override string RequestUrl
        {
            get
            {
                var url = string.Concat(base.RequestUrl, "&type=alphabeticalByName&size=500");
                if (!string.IsNullOrEmpty(MusicFolderId))
                {
                    url = string.Concat(url, string.Format("&musicFolderId={0}", MusicFolderId));
                }
                return url;
            }
        }

        public override string ResourcePath
        {
            get
            {
                return "getAlbumList2.view";
            }
        }

        #endregion

        #region Methods

        public override void HandleResponse(XDocument xDocument)
        {
            var response = xDocument.Element(Namespace + "subsonic-response");
            var albumList = response?.Element(Namespace + "albumList2");

            if (albumList == null)
            {
                Result = new List<Album>();
                return;
            }

            var xmlSerializer = new XmlSerializer(typeof(Album));
            var albums = albumList.Elements(Namespace + "album")
                .Select(albumElement =>
                {
                    using (var xmlReader = albumElement.CreateReader())
                    {
                        return (Album)xmlSerializer.Deserialize(xmlReader);
                    }
                }).ToList();

            Result = albums;
        }

        #endregion
    }
}
