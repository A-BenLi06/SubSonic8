namespace Subsonic8.Phone.Views
{
    using System;
    using System.Collections.ObjectModel;
    using System.Threading.Tasks;
    using Client.Common.Models.Subsonic;
    using Client.Common.Services;
    using Client.Common.Services.DataStructures.SubsonicService;
    using Windows.Storage;
    using Windows.UI.Popups;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Navigation;

    public sealed partial class MainPage : Page
    {
        private ISubsonicService _subsonicService;

        // Control references
        private ProgressRing _loadingIndicator;
        private ListView _albumListView;
        private TextBlock _emptyStateText;

        public ObservableCollection<Album> Albums { get; } = new ObservableCollection<Album>();

        public MainPage()
        {
            InitializeComponent();
            DataContext = this;

            // Get control references
            _loadingIndicator = FindName("LoadingIndicator") as ProgressRing;
            _albumListView = FindName("AlbumListView") as ListView;
            _emptyStateText = FindName("EmptyStateText") as TextBlock;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            await InitializeService();
            await LoadAlbums();
        }

        private async Task InitializeService()
        {
            var configuration = await LoadConfiguration();
            _subsonicService = new SubsonicService();
            _subsonicService.Configuration = configuration;
        }

        private async Task<SubsonicServiceConfiguration> LoadConfiguration()
        {
            var config = new SubsonicServiceConfiguration();

            try
            {
                var settings = ApplicationData.Current.LocalSettings;
                if (settings.Values.ContainsKey("BaseUrl"))
                {
                    config.BaseUrl = settings.Values["BaseUrl"] as string;
                    config.Username = settings.Values["Username"] as string;
                    config.Password = settings.Values["Password"] as string;
                    config.CompatibleMode = settings.Values.ContainsKey("CompatibleMode") 
                        && (bool)settings.Values["CompatibleMode"];
                }
            }
            catch
            {
                // Use defaults
            }

            return config;
        }

        private async Task LoadAlbums()
        {
            if (string.IsNullOrEmpty(_subsonicService.Configuration?.BaseUrl))
            {
                if (_emptyStateText != null) _emptyStateText.Visibility = Visibility.Visible;
                if (_albumListView != null) _albumListView.Visibility = Visibility.Collapsed;
                return;
            }

            if (_loadingIndicator != null) _loadingIndicator.IsActive = true;
            if (_emptyStateText != null) _emptyStateText.Visibility = Visibility.Collapsed;

            try
            {
                var result = _subsonicService.GetAlbumList(string.Empty);
                await result.Execute();

                if (result.Error == null && result.Result != null)
                {
                    Albums.Clear();
                    foreach (var album in result.Result)
                    {
                        album.CoverArt = _subsonicService.GetCoverArtForId(album.CoverArt);
                        Albums.Add(album);
                    }
                }

                if (_emptyStateText != null) _emptyStateText.Visibility = Albums.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                if (_albumListView != null) _albumListView.Visibility = Albums.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                await new MessageDialog("Failed to load albums: " + ex.Message).ShowAsync();
            }
            finally
            {
                if (_loadingIndicator != null) _loadingIndicator.IsActive = false;
            }
        }

        private void OnAlbumClick(object sender, ItemClickEventArgs e)
        {
            var album = e.ClickedItem as Album;
            if (album != null)
            {
                Frame.Navigate(typeof(AlbumPage), album.Id);
            }
        }

        private void OnSettingsClick(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SettingsPage));
        }

        private void OnSearchClick(object sender, RoutedEventArgs e)
        {
            // TODO: Navigate to search page
        }

        private async void OnRefreshClick(object sender, RoutedEventArgs e)
        {
            await LoadAlbums();
        }
    }
}
