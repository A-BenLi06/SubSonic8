namespace Subsonic8.Phone.Views
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Threading.Tasks;
    using Subsonic8.Phone.Services;
    using Windows.Foundation.Collections;
    using Windows.Media.Playback;
    using Windows.Phone.UI.Input;
    using Windows.Storage;
    using Windows.UI.Core;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Controls.Primitives;
    using Windows.UI.Xaml.Media.Animation;
    using Windows.UI.Xaml.Media.Imaging;
    using Windows.UI.Xaml.Navigation;

    /// <summary>
    /// Navigation parameter for playback with playlist support.
    /// </summary>
    public class PlaybackNavigationParameter
    {
        public List<SongDisplayItem> Playlist { get; set; }
        public int StartIndex { get; set; }
    }

    public sealed partial class PlaybackPage : Page
    {
        private WP8SubsonicService _subsonicService;
        private List<SongDisplayItem> _playlist;
        private DispatcherTimer _progressTimer;
        private bool _isFirstLoad = true;

        // Control references
        private TextBlock _songTitleText;
        private TextBlock _artistText;
        private TextBlock _currentTimeText;
        private TextBlock _totalTimeText;
        private Slider _progressSlider;
        private Button _playPauseButton;
        private Image _albumArtImage;
        private Image _backgroundImage;
        
        // Animation references
        private Storyboard _fadeOutStoryboard;
        private Storyboard _fadeInStoryboard;
        private Storyboard _backgroundFadeStoryboard;

        public PlaybackPage()
        {
            InitializeComponent();
            
            // Get control references
            _songTitleText = FindName("SongTitleText") as TextBlock;
            _artistText = FindName("ArtistText") as TextBlock;
            _currentTimeText = FindName("CurrentTimeText") as TextBlock;
            _totalTimeText = FindName("TotalTimeText") as TextBlock;
            _progressSlider = FindName("ProgressSlider") as Slider;
            _playPauseButton = FindName("PlayPauseButton") as Button;
            _albumArtImage = FindName("AlbumArtImage") as Image;
            _backgroundImage = FindName("BackgroundImage") as Image;

            _progressTimer = new DispatcherTimer();
            _progressTimer.Interval = TimeSpan.FromSeconds(1);
            _progressTimer.Tick += OnProgressTimerTick;
            
            // Get animation references
            _fadeOutStoryboard = Resources["FadeOutStoryboard"] as Storyboard;
            _fadeInStoryboard = Resources["FadeInStoryboard"] as Storyboard;
            _backgroundFadeStoryboard = Resources["BackgroundFadeStoryboard"] as Storyboard;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            HardwareButtons.BackPressed += OnHardwareBackPressed;

            InitializeService();

            // Subscribe to background audio events
            BackgroundMediaPlayer.MessageReceivedFromBackground += OnMessageReceivedFromBackground;
            BackgroundMediaPlayer.Current.CurrentStateChanged += OnMediaPlayerStateChanged;

            // Handle navigation parameter
            var navParam = e.Parameter as PlaybackNavigationParameter;
            if (navParam != null)
            {
                _playlist = navParam.Playlist;
                await StartBackgroundPlayback(navParam.Playlist, navParam.StartIndex);
            }
            else
            {
                var songId = e.Parameter as string;
                if (!string.IsNullOrEmpty(songId))
                {
                    // Single song
                    var list = new List<SongDisplayItem> { new SongDisplayItem { Id = songId } };
                    _playlist = list;
                    await StartBackgroundPlayback(list, 0);
                }
            }
            
            UpdatePlayPauseButton();
            _progressTimer.Start();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            HardwareButtons.BackPressed -= OnHardwareBackPressed;
            
            // Unsubscribe but DO NOT stop playback
            BackgroundMediaPlayer.MessageReceivedFromBackground -= OnMessageReceivedFromBackground;
            BackgroundMediaPlayer.Current.CurrentStateChanged -= OnMediaPlayerStateChanged;
            _progressTimer.Stop();
        }

        private void OnHardwareBackPressed(object sender, BackPressedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                e.Handled = true;
                Frame.GoBack();
            }
        }

        private void InitializeService()
        {
            var settings = ApplicationData.Current.LocalSettings;
            var baseUrl = settings.Values.ContainsKey("BaseUrl") ? settings.Values["BaseUrl"] as string : "";
            var username = settings.Values.ContainsKey("Username") ? settings.Values["Username"] as string : "";
            var password = settings.Values.ContainsKey("Password") ? settings.Values["Password"] as string : "";
            var compatibleMode = settings.Values.ContainsKey("CompatibleMode") && (bool)settings.Values["CompatibleMode"];

            _subsonicService = new WP8SubsonicService();
            _subsonicService.Configure(baseUrl, username, password);
            _subsonicService.CompatibleMode = compatibleMode;
        }

        private async Task StartBackgroundPlayback(List<SongDisplayItem> playlist, int startIndex)
        {
            // Convert to TrackInfo list
            var tracks = new List<TrackInfo>();
            foreach (var item in playlist)
            {
                var url = _subsonicService.GetStreamUri(item.Id).ToString();
                var coverUrl = !string.IsNullOrEmpty(item.CoverArt) ? _subsonicService.GetCoverArtUrl(item.CoverArt) : "";

                tracks.Add(new TrackInfo
                {
                    Id = item.Id,
                    Title = item.Title,
                    Artist = item.Artist,
                    CoverArt = coverUrl, // Use full URL for background task
                    StreamUrl = url,
                    Duration = item.Duration
                });
            }

            // Serialize playlist
            string jsonPlaylist = "";
            var serializer = new DataContractJsonSerializer(typeof(List<TrackInfo>));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, tracks);
                stream.Position = 0;
                using (var reader = new StreamReader(stream))
                {
                    jsonPlaylist = reader.ReadToEnd();
                }
            }

            // Send UpdatePlaylist message
            var message = new ValueSet();
            message.Add(BackgroundAudioMessages.UpdatePlaylist, jsonPlaylist);
            BackgroundMediaPlayer.SendMessageToBackground(message);

            // Send StartPlayback message
            var startMessage = new ValueSet();
            startMessage.Add(BackgroundAudioMessages.StartPlayback, "");
            startMessage.Add(BackgroundAudioMessages.CurrentIndex, startIndex);
            BackgroundMediaPlayer.SendMessageToBackground(startMessage);
        }

        private async void OnMessageReceivedFromBackground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                foreach (var key in e.Data.Keys)
                {
                    if (key == BackgroundAudioMessages.TrackChanged)
                    {
                        var trackId = e.Data[key] as string;
                        UpdateUIForTrack(trackId);
                    }
                }
            });
        }

        private async void OnMediaPlayerStateChanged(MediaPlayer sender, object args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                UpdatePlayPauseButton();
            });
        }

        private void UpdateUIForTrack(string trackId)
        {
            if (_playlist == null) return;
            var song = _playlist.FirstOrDefault(x => x.Id == trackId);
            if (song == null) return;

            // Animations
            if (!_isFirstLoad && _fadeOutStoryboard != null)
            {
                _fadeOutStoryboard.Begin();
            }

            // Update Text
            if (_songTitleText != null) _songTitleText.Text = song.Title ?? "Unknown";
            if (_artistText != null) _artistText.Text = song.Artist ?? "Unknown Artist";
            if (_totalTimeText != null) _totalTimeText.Text = song.DurationString ?? FormatTime(song.Duration);
            if (_progressSlider != null) _progressSlider.Maximum = song.Duration > 0 ? song.Duration : 300;

            // Update Image
            if (!string.IsNullOrEmpty(song.CoverArt) && _albumArtImage != null)
            {
                var coverUrl = _subsonicService.GetCoverArtUrl(song.CoverArt);
                var image = new BitmapImage(new Uri(coverUrl));
                _albumArtImage.Source = image;
                if (_backgroundImage != null) _backgroundImage.Source = image;
                
                if (_backgroundFadeStoryboard != null) _backgroundFadeStoryboard.Begin();
            }

            // Fade In
            if (_fadeInStoryboard != null)
            {
                // Delay slightly to allow FadeOut to be visible if it was started
                 // Ideally we'd await, but this is void.
                 // The Storyboard is async but Begin() is not.
                 // We rely on the visual transition.
                 _fadeInStoryboard.Begin();
            }
            _isFirstLoad = false;
        }

        private string FormatTime(double totalSeconds)
        {
            var minutes = (int)(totalSeconds / 60);
            var seconds = (int)(totalSeconds % 60);
            return string.Format("{0}:{1:D2}", minutes, seconds);
        }

        private void OnProgressTimerTick(object sender, object e)
        {
            try 
            {
                var position = BackgroundMediaPlayer.Current.Position.TotalSeconds;
                if (_currentTimeText != null) _currentTimeText.Text = FormatTime(position);
                if (_progressSlider != null && !double.IsNaN(position)) _progressSlider.Value = position;
            }
            catch {}
        }

        private void UpdatePlayPauseButton()
        {
            if (_playPauseButton != null)
            {
                var textBlock = _playPauseButton.Content as TextBlock;
                if (textBlock != null)
                {
                    var state = BackgroundMediaPlayer.Current.CurrentState;
                    bool isPlaying = state == MediaPlayerState.Playing || state == MediaPlayerState.Buffering || state == MediaPlayerState.Opening;
                    textBlock.Text = isPlaying ? "\uE103" : "\uE102";
                }
            }
        }

        private void OnPlayPauseClick(object sender, RoutedEventArgs e)
        {
            var state = BackgroundMediaPlayer.Current.CurrentState;
            if (state == MediaPlayerState.Playing)
            {
                SendMessageToBackground(BackgroundAudioMessages.Pause);
            }
            else
            {
                SendMessageToBackground(BackgroundAudioMessages.Play);
            }
        }

        private void OnPreviousClick(object sender, RoutedEventArgs e)
        {
            SendMessageToBackground(BackgroundAudioMessages.Previous);
        }

        private void OnNextClick(object sender, RoutedEventArgs e)
        {
            SendMessageToBackground(BackgroundAudioMessages.Next);
        }

        private void OnProgressSliderChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (Math.Abs(e.NewValue - BackgroundMediaPlayer.Current.Position.TotalSeconds) > 2)
            {
                BackgroundMediaPlayer.Current.Position = TimeSpan.FromSeconds(e.NewValue);
            }
        }

        private void SendMessageToBackground(string messageName)
        {
            var message = new ValueSet();
            message.Add(messageName, "");
            BackgroundMediaPlayer.SendMessageToBackground(message);
        }
    }
}
