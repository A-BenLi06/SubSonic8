using System.Runtime.Serialization;

namespace Subsonic8.Phone.Services
{
    public static class BackgroundAudioMessages
    {
        // Message Types
        public const string Play = "Play";
        public const string Pause = "Pause";
        public const string PlayPause = "PlayPause";
        public const string Stop = "Stop";
        public const string Next = "Next";
        public const string Previous = "Previous";
        public const string UpdatePlaylist = "UpdatePlaylist"; // To set a new playlist
        public const string TrackChanged = "TrackChanged";
        public const string StateChanged = "StateChanged";
        public const string AppResumed = "AppResumed";
        public const string AppSuspended = "AppSuspended";
        public const string StartPlayback = "StartPlayback";
        
        // Payload Keys
        public const string TrackId = "TrackId";
        public const string Position = "Position";
        public const string Duration = "Duration";
        public const string PlayerState = "PlayerState";
        public const string Playlist = "Playlist"; // Json serialized playlist
        public const string CurrentIndex = "CurrentIndex";
    }

    [DataContract]
    public class TrackInfo
    {
        [DataMember]
        public string Id { get; set; }
        
        [DataMember]
        public string Title { get; set; }
        
        [DataMember]
        public string Artist { get; set; }
        
        [DataMember]
        public string Album { get; set; }
        
        [DataMember]
        public string CoverArt { get; set; }
        
        [DataMember]
        public string StreamUrl { get; set; }
        
        [DataMember]
        public int Duration { get; set; }
    }
}
