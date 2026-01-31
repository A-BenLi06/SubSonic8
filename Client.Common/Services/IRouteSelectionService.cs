namespace Client.Common.Services
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Service for selecting the best route (URL) to the Subsonic server.
    /// </summary>
    public interface IRouteSelectionService
    {
        /// <summary>
        /// Tests the provided URLs and returns the best available route.
        /// </summary>
        /// <param name="primaryUrl">The primary server URL.</param>
        /// <param name="secondaryUrl">The secondary/fallback server URL.</param>
        /// <param name="username">Username for authentication.</param>
        /// <param name="password">Password for authentication.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A result containing the selected URL or failure information.</returns>
        Task<RouteSelectionResult> SelectBestRouteAsync(
            string primaryUrl,
            string secondaryUrl,
            string username,
            string password,
            CancellationToken cancellationToken = default(CancellationToken));
    }

    /// <summary>
    /// Result of a route selection operation.
    /// </summary>
    public class RouteSelectionResult
    {
        /// <summary>
        /// Gets or sets whether the route selection was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the selected URL (only valid if Success is true).
        /// </summary>
        public string SelectedUrl { get; set; }

        /// <summary>
        /// Gets or sets the failure reason (only valid if Success is false).
        /// </summary>
        public string FailureReason { get; set; }
    }
}
