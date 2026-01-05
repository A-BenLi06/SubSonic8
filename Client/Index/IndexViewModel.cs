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

    public class IndexViewModel : DetailViewModelBase<IndexItem>, IIndexViewModel
    {
        #region Constructors and Destructors

        public IndexViewModel()
        {
            MenuItems = new BindableCollection<MenuItemViewModel>();
        }

        #endregion

        #region Methods

        protected override async Task AfterPopulate(string id)
        {
            var result = SubsonicService.GetMusicFolders();
            await result.WithErrorHandler(ErrorDialogViewModel).OnSuccess(r => SetIndexName(r, id)).Execute();
        }

        protected override IEnumerable<IMediaModel> GetItemsToDisplay(IndexItem result)
        {
            return result.Artists;
        }

        protected override IServiceResultBase<IndexItem> GetResult(string id)
        {
            return SubsonicService.GetIndex(id);
        }

        private void SetIndexName(IEnumerable<MusicFolder> musicFolders, string id)
        {
            if (Item == null)
            {
                return;
            }

            if (musicFolders == null || !musicFolders.Any())
            {
                Item.Name = "Music";
                return;
            }

            var rootFolder = musicFolders.FirstOrDefault(f => f != null && f.Id == id);
            Item.Name = rootFolder?.Name ?? "Music";
        }

        #endregion
    }
}