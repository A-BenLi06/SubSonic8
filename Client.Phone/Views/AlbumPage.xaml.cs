namespace Subsonic8.Phone.Views
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using Client.Common.Models.Subsonic;
    using Client.Common.Services;
    using Client.Common.Services.DataStructures.SubsonicService;
    using Windows.Phone.UI.Input;
    using Windows.Storage;
    using Windows.UI.Popups;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Navigation;

    public sealed partial class AlbumPage : Page
    {
        private ISubsonicService _subsonicService;
        private string _albumId;
        private Album _album;

        // Control references
        private ProgressRing _loadingIndicator;
        private TextBlock _albumNameText;
        private TextBlock _artistNameText;
        private TextBlock _songCountText;
        private ListView _songListView;

        public ObservableCollection<SongDisplayItem> Songs { get; set; }

        public AlbumPage()
        {
            Songs = new ObservableCollection<SongDisplayItem>();
            InitializeComponent();
            DataContext = this;

            // Get control references after InitializeComponent
            _loadingIndicator = FindName("LoadingIndicator") as ProgressRing;
            _albumNameText = FindName("AlbumNameText") as TextBlock;
            _artistNameText = FindName("ArtistNameText") as TextBlock;
            _songCountText = FindName("SongCountText") as TextBlock;
            _songListView = FindName("SongListView") as ListView;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            HardwareButtons.BackPressed += OnHardwareBackPressed;

            _albumId = e.Parameter as string;
            await InitializeService();
            await LoadAlbum();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            HardwareButtons.BackPressed -= OnHardwareBackPressed;
        }

        private void OnHardwareBackPressed(object sender, BackPressedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                e.Handled = true;
                Frame.GoBack();
            }
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

        private async Task LoadAlbum()
        {
            if (string.IsNullOrEmpty(_albumId))
            {
                return;
            }

            if (_loadingIndicator != null) _loadingIndicator.IsActive = true;

            try
            {
                var result = _subsonicService.GetAlbum(_albumId);
                await result.Execute();

                if (result.Error == null && result.Result != null)
                {
                    _album = result.Result;
                    if (_albumNameText != null) _albumNameText.Text = _album.Name ?? "Unknown Album";
                    if (_artistNameText != null) _artistNameText.Text = _album.Artist ?? "Unknown Artist";

                    Songs.Clear();
                    if (_album.Songs != null)
                    {
                        int trackNum = 1;
                        foreach (var song in _album.Songs)
                        {
                            Songs.Add(new SongDisplayItem
                            {
                                Id = song.Id,
                                Track = trackNum++,
                                Title = song.Title ?? song.Name ?? "Unknown",
                                Artist = song.Artist ?? "Unknown Artist",
                                Duration = song.Duration,
                                DurationString = FormatDuration(song.Duration),
                                CoverArt = song.CoverArt ?? _album.CoverArt
                            });
                        }
                    }

                    if (_songCountText != null) _songCountText.Text = string.Format("{0} songs", Songs.Count);
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog("Failed to load album: " + ex.Message).ShowAsync();
            }
            finally
            {
                if (_loadingIndicator != null) _loadingIndicator.IsActive = false;
            }
        }

        private string FormatDuration(int seconds)
        {
            var minutes = seconds / 60;
            var secs = seconds % 60;
            return string.Format("{0}:{1:D2}", minutes, secs);
        }

        private void NavigateToPlayback(int startIndex)
        {
            if (Songs.Count == 0) return;

            var navParam = new PlaybackNavigationParameter
            {
                Playlist = Songs.ToList(),
                StartIndex = startIndex
            };
            Frame.Navigate(typeof(PlaybackPage), navParam);
        }

        private void OnSongClick(object sender, ItemClickEventArgs e)
        {
            var song = e.ClickedItem as SongDisplayItem;
            if (song != null)
            {
                var index = Songs.IndexOf(song);
                NavigateToPlayback(index >= 0 ? index : 0);
            }
        }

        private void OnPlayAllClick(object sender, RoutedEventArgs e)
        {
            NavigateToPlayback(0);
        }

        private void OnShuffleClick(object sender, RoutedEventArgs e)
        {
            if (Songs.Count > 0)
            {
                var random = new Random();
                var shuffledList = Songs.OrderBy(x => random.Next()).ToList();
                var navParam = new PlaybackNavigationParameter
                {
                    Playlist = shuffledList,
                    StartIndex = 0
                };
                Frame.Navigate(typeof(PlaybackPage), navParam);
            }
        }
    }

    public class SongDisplayItem
    {
        public string Id { get; set; }
        public int Track { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public int Duration { get; set; }
        public string DurationString { get; set; }
        public string CoverArt { get; set; }
    }
}
