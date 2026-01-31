namespace Client.Common.Services
{
    using System;
    using Windows.Networking.Connectivity;

    /// <summary>
    /// Network detection service implementation using Windows.Networking.Connectivity.
    /// </summary>
    public class NetworkDetectionService : INetworkDetectionService
    {
        /// <inheritdoc />
        public event EventHandler<NetworkConnectionType> NetworkChanged;

        public NetworkDetectionService()
        {
            NetworkInformation.NetworkStatusChanged += OnNetworkStatusChanged;
        }

        /// <inheritdoc />
        public NetworkConnectionType GetCurrentConnectionType()
        {
            try
            {
                var profile = NetworkInformation.GetInternetConnectionProfile();
                if (profile == null)
                {
                    return NetworkConnectionType.None;
                }

                var interfaceType = profile.NetworkAdapter?.IanaInterfaceType;

                // IanaInterfaceType values:
                // 6 = Ethernet
                // 71 = WiFi
                // 243, 244 = Mobile/Cellular (3G/4G/LTE)
                // See: https://docs.microsoft.com/en-us/uwp/api/windows.networking.connectivity.networkadapter.ianainterfacetype

                switch (interfaceType)
                {
                    case 6:
                        return NetworkConnectionType.Ethernet;
                    case 71:
                        return NetworkConnectionType.WiFi;
                    case 243:
                    case 244:
                        return NetworkConnectionType.Cellular;
                    default:
                        // Check if it's a WWAN (mobile) connection
                        if (profile.IsWwanConnectionProfile)
                        {
                            return NetworkConnectionType.Cellular;
                        }
                        if (profile.IsWlanConnectionProfile)
                        {
                            return NetworkConnectionType.WiFi;
                        }
                        return NetworkConnectionType.Unknown;
                }
            }
            catch
            {
                return NetworkConnectionType.Unknown;
            }
        }

        private void OnNetworkStatusChanged(object sender)
        {
            var currentType = GetCurrentConnectionType();
            NetworkChanged?.Invoke(this, currentType);
        }
    }
}
