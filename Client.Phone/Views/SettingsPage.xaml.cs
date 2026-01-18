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
        private TextBox _serverUrlTextBox;
        private TextBox _usernameTextBox;
        private PasswordBox _passwordBox;
        private ToggleSwitch _compatibleModeToggle;
        private TextBlock _statusText;

        public SettingsPage()
        {
            InitializeComponent();

            // Get control references
            _serverUrlTextBox = FindName("ServerUrlTextBox") as TextBox;
            _usernameTextBox = FindName("UsernameTextBox") as TextBox;
            _passwordBox = FindName("PasswordBox") as PasswordBox;
            _compatibleModeToggle = FindName("CompatibleModeToggle") as ToggleSwitch;
            _statusText = FindName("StatusText") as TextBlock;
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

                if (settings.Values.ContainsKey("BaseUrl") && _serverUrlTextBox != null)
                {
                    _serverUrlTextBox.Text = settings.Values["BaseUrl"] as string ?? string.Empty;
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

        private void SaveSettings()
        {
            var settings = ApplicationData.Current.LocalSettings;
            if (_serverUrlTextBox != null) settings.Values["BaseUrl"] = _serverUrlTextBox.Text.Trim();
            if (_usernameTextBox != null) settings.Values["Username"] = _usernameTextBox.Text.Trim();
            if (_passwordBox != null) settings.Values["Password"] = _passwordBox.Password;
            if (_compatibleModeToggle != null) settings.Values["CompatibleMode"] = _compatibleModeToggle.IsOn;
        }



        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            if (_statusText != null) _statusText.Text = "Settings saved successfully!";

            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private async void OnTestConnectionClick(object sender, RoutedEventArgs e)
        {
            if (_statusText != null) _statusText.Text = "Testing connection...";

            var wp8Service = new Subsonic8.Phone.Services.WP8SubsonicService();
            wp8Service.Configure(
                _serverUrlTextBox?.Text.Trim() ?? string.Empty,
                _usernameTextBox?.Text.Trim() ?? string.Empty,
                _passwordBox?.Password ?? string.Empty
            );

            try
            {
                var success = await wp8Service.PingAsync();

                if (success)
                {
                    if (_statusText != null) _statusText.Text = "✓ Connection successful!";
                }
                else
                {
                    if (_statusText != null) _statusText.Text = "✗ Connection failed: Ping returned false";
                }
            }
            catch (Exception ex)
            {
                if (_statusText != null) _statusText.Text = "✗ Error: " + ex.Message;
            }
            finally
            {
                wp8Service.Dispose();
            }
        }
    }
}
