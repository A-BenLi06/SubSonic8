namespace Subsonic8.Index
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Caliburn.Micro;
    using Client.Common.Models;
    using Client.Common.Models.Subsonic;
    using Client.Common.Results;
    using Subsonic8.Framework.ViewModel;
    using Subsonic8.MenuItem;

    public class IndexViewModel : CollectionViewModelBase<string, IList<Album>>, IIndexViewModel
    {
        #region Fields

        private string _indexName;

        #endregion

        #region Constructors and Destructors

        public IndexViewModel()
        {
            MenuItems = new BindableCollection<MenuItemViewModel>();
            UpdateDisplayName = () => DisplayName = _indexName ?? "Albums";
        }

        #endregion

        #region Methods

        protected override async Task AfterPopulate(string id)
        {
            var result = SubsonicService.GetMusicFolders();
            await result.WithErrorHandler(ErrorDialogViewModel).OnSuccess(r => SetIndexName(r, id)).Execute();
        }

        protected override IEnumerable<IMediaModel> GetItemsToDisplay(IList<Album> result)
        {
            return result;
        }

        protected override IServiceResultBase<IList<Album>> GetResult(string id)
        {
            return SubsonicService.GetAlbumList(id);
        }

        private void SetIndexName(IEnumerable<MusicFolder> musicFolders, string id)
        {
            if (musicFolders == null || !musicFolders.Any())
            {
                _indexName = "Albums";
                return;
            }

            var rootFolder = musicFolders.FirstOrDefault(f => f != null && f.Id == id);
            _indexName = rootFolder?.Name ?? "Albums";
            UpdateDisplayName();
        }

        #endregion
    }
}