namespace Client.Common.Services
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Client.Common.Services.DataStructures.SubsonicService;

    /// <summary>
    /// Service that selects the best route (URL) to the Subsonic server by testing connectivity.
    /// </summary>
    public class RouteSelectionService : IRouteSelectionService
    {
        private readonly INetworkDetectionService _networkDetectionService;
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

        public RouteSelectionService(INetworkDetectionService networkDetectionService)
        {
            _networkDetectionService = networkDetectionService;
        }

        /// <inheritdoc />
        public async Task<RouteSelectionResult> SelectBestRouteAsync(
            string primaryUrl,
            string secondaryUrl,
            string username,
            string password,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var hasPrimary = !string.IsNullOrWhiteSpace(primaryUrl);
            var hasSecondary = !string.IsNullOrWhiteSpace(secondaryUrl);

            // No URLs configured
            if (!hasPrimary && !hasSecondary)
            {
                return new RouteSelectionResult
                {
                    Success = false,
                    FailureReason = "No server URLs configured"
                };
            }

            // Only one URL configured - just test that one
            if (hasPrimary && !hasSecondary)
            {
                return await TestSingleUrlAsync(primaryUrl, username, password, cancellationToken);
            }
            if (hasSecondary && !hasPrimary)
            {
                return await TestSingleUrlAsync(secondaryUrl, username, password, cancellationToken);
            }

            // Both URLs configured - check network type
            var networkType = _networkDetectionService.GetCurrentConnectionType();

            // On cellular network (Windows only optimization): only test secondary (assumed DDNS/external)
            // Note: On WP, we always test both addresses as per user requirement
#if WINDOWS_APP
            if (networkType == NetworkConnectionType.Cellular)
            {
                return await TestSingleUrlAsync(secondaryUrl, username, password, cancellationToken);
            }
#endif

            // WiFi/Ethernet/Unknown: test both URLs concurrently
            return await TestBothUrlsAsync(primaryUrl, secondaryUrl, username, password, cancellationToken);
        }

        private async Task<RouteSelectionResult> TestSingleUrlAsync(
            string url,
            string username,
            string password,
            CancellationToken cancellationToken)
        {
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                cts.CancelAfter(Timeout);

                try
                {
                    var success = await PingUrlAsync(url, username, password, cts.Token);
                    if (success)
                    {
                        return new RouteSelectionResult
                        {
                            Success = true,
                            SelectedUrl = url
                        };
                    }

                    return new RouteSelectionResult
                    {
                        Success = false,
                        FailureReason = "Server ping failed"
                    };
                }
                catch (OperationCanceledException)
                {
                    return new RouteSelectionResult
                    {
                        Success = false,
                        FailureReason = "Connection timeout"
                    };
                }
                catch (Exception ex)
                {
                    return new RouteSelectionResult
                    {
                        Success = false,
                        FailureReason = ex.Message
                    };
                }
            }
        }

        private async Task<RouteSelectionResult> TestBothUrlsAsync(
            string primaryUrl,
            string secondaryUrl,
            string username,
            string password,
            CancellationToken cancellationToken)
        {
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                cts.CancelAfter(Timeout);

                var tasks = new List<Task<PingTaskResult>>
                {
                    PingUrlWithResultAsync(primaryUrl, username, password, cts.Token),
                    PingUrlWithResultAsync(secondaryUrl, username, password, cts.Token)
                };

                // Use WhenAny pattern to get first successful result
                while (tasks.Count > 0)
                {
                    var completedTask = await Task.WhenAny(tasks);
                    tasks.Remove(completedTask);

                    try
                    {
                        var result = await completedTask;
                        if (result.Success)
                        {
                            // Cancel remaining tasks
                            cts.Cancel();
                            return new RouteSelectionResult
                            {
                                Success = true,
                                SelectedUrl = result.Url
                            };
                        }
                    }
                    catch
                    {
                        // Task failed, continue waiting for others
                    }
                }

                return new RouteSelectionResult
                {
                    Success = false,
                    FailureReason = "All routes failed or timed out"
                };
            }
        }

        private async Task<bool> PingUrlAsync(
            string baseUrl,
            string username,
            string password,
            CancellationToken cancellationToken)
        {
            var config = new SubsonicServiceConfiguration
            {
                BaseUrl = baseUrl,
                Username = username,
                Password = password
            };

            var pingUrl = BuildPingUrl(config);

            using (var client = new HttpClient { Timeout = Timeout })
            {
                try
                {
                    var response = await client.GetAsync(pingUrl, cancellationToken);
                    return response.IsSuccessStatusCode;
                }
                catch
                {
                    return false;
                }
            }
        }

        private async Task<PingTaskResult> PingUrlWithResultAsync(
            string baseUrl,
            string username,
            string password,
            CancellationToken cancellationToken)
        {
            var success = await PingUrlAsync(baseUrl, username, password, cancellationToken);
            return new PingTaskResult { Url = baseUrl, Success = success };
        }

        private static string BuildPingUrl(ISubsonicServiceConfiguration config)
        {
            // Build the ping.view URL with authentication parameters
            var baseUrl = (config.BaseUrl ?? string.Empty).TrimEnd('/');
            var username = config.Username ?? string.Empty;
            return string.Format(
                "{0}/rest/ping.view?u={1}&p={2}&c=SubSonic8&v=1.13.0",
                baseUrl,
                Uri.EscapeDataString(username),
                config.EncodedPassword);
        }

        private class PingTaskResult
        {
            public string Url { get; set; }
            public bool Success { get; set; }
        }
    }
}
