using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage.Streams;

namespace Client.Phone.BackgroundAudioTask
{
    public sealed class AudioTask : IBackgroundTask
    {
        private BackgroundTaskDeferral _deferral;
        private SystemMediaTransportControls _systemMediaTransportControls;
        private MediaPlayer _mediaPlayer;
        private List<TrackInfo> _playlist = new List<TrackInfo>();
        private int _currentIndex = -1;
        private AutoResetEvent _sererInitialized = new AutoResetEvent(false);
        private bool _isBackground;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
            _isBackground = true;
            
            _systemMediaTransportControls = SystemMediaTransportControls.GetForCurrentView();
            _systemMediaTransportControls.IsEnabled = true;
            _systemMediaTransportControls.IsPlayEnabled = true;
            _systemMediaTransportControls.IsPauseEnabled = true;
            _systemMediaTransportControls.IsNextEnabled = true;
            _systemMediaTransportControls.IsPreviousEnabled = true;
            _systemMediaTransportControls.ButtonPressed += SystemMediaTransportControls_ButtonPressed;

            _mediaPlayer = BackgroundMediaPlayer.Current;
            _mediaPlayer.CurrentStateChanged += MediaPlayer_CurrentStateChanged;
            _mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
            BackgroundMediaPlayer.MessageReceivedFromForeground += BackgroundMediaPlayer_MessageReceivedFromForeground;

            // Notify foreground that we are ready
            SendMessage(BackgroundAudioMessages.StateChanged, _mediaPlayer.CurrentState.ToString());

            taskInstance.Canceled += TaskInstance_Canceled;
            taskInstance.Task.Completed += Task_Completed;
        }

        private void Task_Completed(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            Shutdown();
        }

        private void TaskInstance_Canceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            Shutdown();
        }

        private void Shutdown()
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.CurrentStateChanged -= MediaPlayer_CurrentStateChanged;
                _mediaPlayer.MediaEnded -= MediaPlayer_MediaEnded;
                BackgroundMediaPlayer.MessageReceivedFromForeground -= BackgroundMediaPlayer_MessageReceivedFromForeground;
            }

            if (_systemMediaTransportControls != null)
            {
                _systemMediaTransportControls.ButtonPressed -= SystemMediaTransportControls_ButtonPressed;
            }

            if (_deferral != null)
            {
                _deferral.Complete();
                _deferral = null;
            }
        }

        private void BackgroundMediaPlayer_MessageReceivedFromForeground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            foreach (var key in e.Data.Keys)
            {
                switch (key)
                {
                    case BackgroundAudioMessages.Play:
                        Play();
                        break;
                    case BackgroundAudioMessages.Pause:
                        Pause();
                        break;
                    case BackgroundAudioMessages.Next:
                        SkipToNext();
                        break;
                    case BackgroundAudioMessages.Previous:
                        SkipToPrevious();
                        break;
                    case BackgroundAudioMessages.UpdatePlaylist:
                        UpdatePlaylist(e.Data[key] as string);
                        break;
                    case BackgroundAudioMessages.StartPlayback:
                         if (e.Data.ContainsKey(BackgroundAudioMessages.CurrentIndex))
                         {
                             int index = int.Parse(e.Data[BackgroundAudioMessages.CurrentIndex].ToString());
                             PlayTrackAt(index);
                         }
                        break;
                }
            }
        }

        private void UpdatePlaylist(string jsonPlaylist)
        {
            try
            {
                var serializer = new DataContractJsonSerializer(typeof(List<TrackInfo>));
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonPlaylist)))
                {
                    _playlist = (List<TrackInfo>)serializer.ReadObject(stream);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error parsing playlist: " + ex.Message);
            }
        }

        private void PlayTrackAt(int index)
        {
            if (_playlist == null || _playlist.Count == 0) return;
            
            if (index >= 0 && index < _playlist.Count)
            {
                _currentIndex = index;
                var track = _playlist[_currentIndex];
                
                // Update SMTC
                UpdateSystemMediaTransportControls(track);
                
                // Play
                _mediaPlayer.AutoPlay = true;
                _mediaPlayer.SetUriSource(new Uri(track.StreamUrl));
                
                // Send track changed message
                SendMessage(BackgroundAudioMessages.TrackChanged, track.Id);
            }
        }

        private void SkipToNext()
        {
            if (_playlist != null && _playlist.Count > 0)
            {
                int nextIndex = (_currentIndex + 1) % _playlist.Count;
                PlayTrackAt(nextIndex);
            }
        }

        private void SkipToPrevious()
        {
            if (_playlist != null && _playlist.Count > 0)
            {
                // If more than 3 seconds in, restart song
                if (_mediaPlayer.Position.TotalSeconds > 3)
                {
                    _mediaPlayer.Position = TimeSpan.Zero;
                    return;
                }

                int prevIndex = _currentIndex - 1;
                if (prevIndex < 0) prevIndex = _playlist.Count - 1;
                PlayTrackAt(prevIndex);
            }
        }

        private void Play()
        {
            if (_mediaPlayer.CurrentState == MediaPlayerState.Paused || 
                _mediaPlayer.CurrentState == MediaPlayerState.Stopped)
            {
                _mediaPlayer.Play();
            }
            else
            {
                // If nothing is playing but we have a playlist, play current or first
                if (_currentIndex == -1 && _playlist.Count > 0)
                {
                    PlayTrackAt(0);
                }
                else if (_currentIndex >= 0 && _currentIndex < _playlist.Count)
                {
                     PlayTrackAt(_currentIndex);
                }
            }
        }

        private void Pause()
        {
            if (_mediaPlayer.CurrentState == MediaPlayerState.Playing)
            {
                _mediaPlayer.Pause();
            }
        }

        private void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            SkipToNext();
        }

        private void MediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
        {
            if (_systemMediaTransportControls != null)
            {
                switch (sender.CurrentState)
                {
                    case MediaPlayerState.Playing:
                        _systemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Playing;
                        break;
                    case MediaPlayerState.Paused:
                        _systemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Paused;
                        break;
                    case MediaPlayerState.Stopped:
                        _systemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Stopped;
                        break;
                    case MediaPlayerState.Closed:
                        _systemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Closed;
                        break;
                }
            }
            
            SendMessage(BackgroundAudioMessages.StateChanged, sender.CurrentState.ToString());
        }

        private void SystemMediaTransportControls_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    Play();
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    Pause();
                    break;
                case SystemMediaTransportControlsButton.Next:
                    SkipToNext();
                    break;
                case SystemMediaTransportControlsButton.Previous:
                    SkipToPrevious();
                    break;
            }
        }

        private void UpdateSystemMediaTransportControls(TrackInfo track)
        {
            if (_systemMediaTransportControls != null)
            {
                _systemMediaTransportControls.IsEnabled = true;
                _systemMediaTransportControls.DisplayUpdater.Type = MediaPlaybackType.Music;
                _systemMediaTransportControls.DisplayUpdater.MusicProperties.Title = track.Title ?? "";
                _systemMediaTransportControls.DisplayUpdater.MusicProperties.Artist = track.Artist ?? "";
                _systemMediaTransportControls.DisplayUpdater.MusicProperties.AlbumArtist = track.Artist ?? "";
                
                if (!string.IsNullOrEmpty(track.CoverArt))
                {
                    // Note: This is an online URL. SMTC might handle it if it supports http(s) streams.
                    // For best results, we should download it or pass a local path, but let's try URI first.
                    // SMTC DisplayUpdater usually needs a RandomAccessStreamReference.
                    try 
                    {
                       _systemMediaTransportControls.DisplayUpdater.Thumbnail = RandomAccessStreamReference.CreateFromUri(new Uri(track.CoverArt));
                    }
                    catch {}
                }
                
                _systemMediaTransportControls.DisplayUpdater.Update();
            }
        }

        private void SendMessage(string key, string value)
        {
            var message = new ValueSet();
            message.Add(key, value);
            BackgroundMediaPlayer.SendMessageToForeground(message);
        }
    }
}
