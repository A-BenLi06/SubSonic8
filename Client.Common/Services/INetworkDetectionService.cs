namespace Client.Common.Services
{
    using System;

    /// <summary>
    /// Service for detecting network connection type.
    /// </summary>
    public interface INetworkDetectionService
    {
        /// <summary>
        /// Gets the current network connection type.
        /// </summary>
        NetworkConnectionType GetCurrentConnectionType();

        /// <summary>
        /// Event raised when network connection type changes.
        /// </summary>
        event EventHandler<NetworkConnectionType> NetworkChanged;
    }

    /// <summary>
    /// Types of network connections.
    /// </summary>
    public enum NetworkConnectionType
    {
        /// <summary>
        /// Unknown or undetected connection type.
        /// </summary>
        Unknown,

        /// <summary>
        /// WiFi connection.
        /// </summary>
        WiFi,

        /// <summary>
        /// Cellular/mobile data connection.
        /// </summary>
        Cellular,

        /// <summary>
        /// Ethernet/wired connection.
        /// </summary>
        Ethernet,

        /// <summary>
        /// No network connection available.
        /// </summary>
        None
    }
}
