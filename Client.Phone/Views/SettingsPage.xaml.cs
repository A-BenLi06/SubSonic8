namespace Subsonic8.Phone.Views
{
    using System;
    using System.Threading.Tasks;
    using Client.Common.Services;
    using Client.Common.Services.DataStructures.SubsonicService;
    using Windows.Phone.UI.Input;
    using Windows.Storage;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Navigation;

    public sealed partial class SettingsPage : Page
    {
        // Control references
        private TextBox _primaryUrlTextBox;
        private TextBox _secondaryUrlTextBox;
        private TextBox _usernameTextBox;
        private PasswordBox _passwordBox;
        private ToggleSwitch _compatibleModeToggle;
        private TextBlock _statusText;
        private TextBlock _urlValidationMessage;

        public SettingsPage()
        {
            InitializeComponent();

            // Get control references
            _primaryUrlTextBox = FindName("PrimaryUrlTextBox") as TextBox;
            _secondaryUrlTextBox = FindName("SecondaryUrlTextBox") as TextBox;
            _usernameTextBox = FindName("UsernameTextBox") as TextBox;
            _passwordBox = FindName("PasswordBox") as PasswordBox;
            _compatibleModeToggle = FindName("CompatibleModeToggle") as ToggleSwitch;
            _statusText = FindName("StatusText") as TextBlock;
            _urlValidationMessage = FindName("UrlValidationMessage") as TextBlock;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            HardwareButtons.BackPressed += OnHardwareBackPressed;
            LoadSettings();
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

        private void LoadSettings()
        {
            try
            {
                var settings = ApplicationData.Current.LocalSettings;

                if (settings.Values.ContainsKey("PrimaryUrl") && _primaryUrlTextBox != null)
                {
                    _primaryUrlTextBox.Text = settings.Values["PrimaryUrl"] as string ?? string.Empty;
                }

                if (settings.Values.ContainsKey("SecondaryUrl") && _secondaryUrlTextBox != null)
                {
                    _secondaryUrlTextBox.Text = settings.Values["SecondaryUrl"] as string ?? string.Empty;
                }

                // Backwards compatibility: migrate old BaseUrl to PrimaryUrl
                if (!settings.Values.ContainsKey("PrimaryUrl") && settings.Values.ContainsKey("BaseUrl"))
                {
                    if (_primaryUrlTextBox != null)
                    {
                        _primaryUrlTextBox.Text = settings.Values["BaseUrl"] as string ?? string.Empty;
                    }
                }

                if (settings.Values.ContainsKey("Username") && _usernameTextBox != null)
                {
                    _usernameTextBox.Text = settings.Values["Username"] as string ?? string.Empty;
                }

                if (settings.Values.ContainsKey("Password") && _passwordBox != null)
                {
                    _passwordBox.Password = settings.Values["Password"] as string ?? string.Empty;
                }

                if (settings.Values.ContainsKey("CompatibleMode") && _compatibleModeToggle != null)
                {
                    _compatibleModeToggle.IsOn = (bool)settings.Values["CompatibleMode"];
                }
            }
            catch
            {
                // Ignore errors loading settings
            }
        }

        private bool ValidateUrls()
        {
            var primaryUrl = _primaryUrlTextBox?.Text.Trim() ?? string.Empty;
            var secondaryUrl = _secondaryUrlTextBox?.Text.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(primaryUrl) && string.IsNullOrWhiteSpace(secondaryUrl))
            {
                if (_urlValidationMessage != null)
                {
                    _urlValidationMessage.Visibility = Visibility.Visible;
                }
                return false;
            }

            if (_urlValidationMessage != null)
            {
                _urlValidationMessage.Visibility = Visibility.Collapsed;
            }
            return true;
        }

        private void SaveSettings()
        {
            var settings = ApplicationData.Current.LocalSettings;
            if (_primaryUrlTextBox != null) settings.Values["PrimaryUrl"] = _primaryUrlTextBox.Text.Trim();
            if (_secondaryUrlTextBox != null) settings.Values["SecondaryUrl"] = _secondaryUrlTextBox.Text.Trim();
            if (_usernameTextBox != null) settings.Values["Username"] = _usernameTextBox.Text.Trim();
            if (_passwordBox != null) settings.Values["Password"] = _passwordBox.Password;
            if (_compatibleModeToggle != null) settings.Values["CompatibleMode"] = _compatibleModeToggle.IsOn;
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            if (!ValidateUrls())
            {
                return;
            }

            SaveSettings();
            if (_statusText != null) _statusText.Text = "Settings saved successfully!";

            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private async void OnTestConnectionClick(object sender, RoutedEventArgs e)
        {
            if (!ValidateUrls())
            {
                return;
            }

            if (_statusText != null) _statusText.Text = "Testing connection...";

            var primaryUrl = _primaryUrlTextBox?.Text.Trim() ?? string.Empty;
            var secondaryUrl = _secondaryUrlTextBox?.Text.Trim() ?? string.Empty;
            var username = _usernameTextBox?.Text.Trim() ?? string.Empty;
            var password = _passwordBox?.Password ?? string.Empty;

            try
            {
                // Use route selection service to test both URLs
                var networkService = new NetworkDetectionService();
                var routeService = new RouteSelectionService(networkService);

                var result = await routeService.SelectBestRouteAsync(
                    primaryUrl,
                    secondaryUrl,
                    username,
                    password);

                if (result.Success)
                {
                    if (_statusText != null)
                    {
                        _statusText.Text = "✓ Connection successful!\nSelected: " + result.SelectedUrl;
                    }
                }
                else
                {
                    if (_statusText != null)
                    {
                        _statusText.Text = "✗ Connection failed: " + result.FailureReason;
                    }
                }
            }
            catch (Exception ex)
            {
                if (_statusText != null) _statusText.Text = "✗ Error: " + ex.Message;
            }
        }
    }
}

