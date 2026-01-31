namespace Client.Common.Services.DataStructures.SubsonicService
{
    using global::Common.Results;

    public interface ISubsonicServiceConfiguration : IConfiguration
    {
        #region Public Properties

        string EncodedCredentials { get; }

        string Password { get; set; }

        string Username { get; set; }

        string EncodedPassword { get; }

        bool CompatibleMode { get; set; }

        /// <summary>
        /// Primary server URL (e.g., internal IP or DDNS).
        /// </summary>
        string PrimaryUrl { get; set; }

        /// <summary>
        /// Secondary/fallback server URL (e.g., DDNS or internal IP).
        /// </summary>
        string SecondaryUrl { get; set; }

        #endregion
    }
}