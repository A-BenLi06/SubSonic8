namespace Subsonic8.Settings
{
    using System.Threading.Tasks;
    using Caliburn.Micro;
    using Client.Common.Services;
    using MugenInjection.Attributes;
    using Subsonic8.Framework.Interfaces;
    using Subsonic8.Framework.Services;
    using Subsonic8.Main;
    using Windows.Security.Credentials;
    using Windows.UI.Xaml.Controls;

    public class SettingsViewModel : Screen
    {
        #region Fields

        private readonly ICustomFrameAdapter _navigationService;

        private readonly IToastNotificationService _notificationService;

        private readonly IStorageService _storageService;

        private readonly ISubsonicService _subsonicService;

        private Subsonic8Configuration _configuration;

        #endregion

        #region Constructors and Destructors

        public SettingsViewModel(
            ISubsonicService subsonicService,
            IToastNotificationService notificationService,
            IStorageService storageService,
            ICustomFrameAdapter navigationService)
        {
            _subsonicService = subsonicService;
            _notificationService = notificationService;
            _storageService = storageService;
            _navigationService = navigationService;
            DisplayName = "Settings";
        }

        #endregion

        #region Public Properties

        public bool CanApplyChanges
        {
            get
            {
                return Configuration != null
                       && !string.IsNullOrWhiteSpace(Configuration.SubsonicServiceConfiguration.Username)
                       && !string.IsNullOrWhiteSpace(Configuration.SubsonicServiceConfiguration.Password)
                       && (!string.IsNullOrWhiteSpace(Configuration.SubsonicServiceConfiguration.PrimaryUrl)
                           || !string.IsNullOrWhiteSpace(Configuration.SubsonicServiceConfiguration.SecondaryUrl));
            }
        }

        public Subsonic8Configuration Configuration
        {
            get
            {
                return _configuration;
            }

            private set
            {
                _configuration = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(() => CanApplyChanges);
            }
        }

        [Inject]
        public ISettingsHelper SettingsHelper { get; set; }

        #endregion

        #region Public Methods and Operators

        public void PasswordChanged(PasswordBox passwordBox)
        {
            Configuration.SubsonicServiceConfiguration.Password = passwordBox.Password;
            NotifyOfPropertyChange(() => CanApplyChanges);
        }

        public async Task Populate()
        {
            var configuration = await _storageService.Load<Subsonic8Configuration>()
                                ?? new Subsonic8Configuration { UseToastNotifications = false };
            PopulateCredentials(configuration);
            Configuration = configuration;
        }

        public async Task ApplyChanges()
        {
            await SaveSettings();
            _navigationService.NavigateToViewModel<MainViewModel>(true);
        }

        public async Task SaveSettings()
        {
            await _storageService.Save(Configuration);
            UpdateCredentials();

            var svcConfig = Configuration.SubsonicServiceConfiguration;

            // Perform route selection to find the best working URL
            if (!string.IsNullOrEmpty(svcConfig.PrimaryUrl) || !string.IsNullOrEmpty(svcConfig.SecondaryUrl))
            {
                var networkService = new NetworkDetectionService();
                var routeService = new RouteSelectionService(networkService);

                var result = await routeService.SelectBestRouteAsync(
                    svcConfig.PrimaryUrl,
                    svcConfig.SecondaryUrl,
                    svcConfig.Username,
                    svcConfig.Password);

                if (result.Success)
                {
                    svcConfig.BaseUrl = result.SelectedUrl;
                }
                else
                {
                    // Fallback: prefer SecondaryUrl (external/DDNS) over PrimaryUrl (internal)
                    svcConfig.BaseUrl = !string.IsNullOrEmpty(svcConfig.SecondaryUrl)
                        ? svcConfig.SecondaryUrl
                        : svcConfig.PrimaryUrl;
                }
            }

            _subsonicService.Configuration = svcConfig;
            _notificationService.EnableNotifications = Configuration.UseToastNotifications;
        }

        public void UsernameChanged(TextBox textBox)
        {
            Configuration.SubsonicServiceConfiguration.Username = textBox.Text;
            NotifyOfPropertyChange(() => CanApplyChanges);
        }

        public void PrimaryUrlChanged(TextBox textBox)
        {
            Configuration.SubsonicServiceConfiguration.PrimaryUrl = textBox.Text;
            NotifyOfPropertyChange(() => CanApplyChanges);
        }

        public void SecondaryUrlChanged(TextBox textBox)
        {
            Configuration.SubsonicServiceConfiguration.SecondaryUrl = textBox.Text;
            NotifyOfPropertyChange(() => CanApplyChanges);
        }

        #endregion

        #region Methods

        protected override async void OnActivate()
        {
            base.OnActivate();

            await Populate();
        }

        private void PopulateCredentials(Subsonic8Configuration configuration)
        {
            var credentialsFromVault = SettingsHelper.GetCredentialsFromVault();
            if (credentialsFromVault == null)
            {
                return;
            }

            configuration.SubsonicServiceConfiguration.Username = credentialsFromVault.UserName;
            configuration.SubsonicServiceConfiguration.Password = credentialsFromVault.Password;
        }

        private void UpdateCredentials()
        {
            var passwordCredential = new PasswordCredential
                                         {
                                             UserName =
                                                 Configuration.SubsonicServiceConfiguration.Username,
                                             Password =
                                                 Configuration.SubsonicServiceConfiguration.Password,
                                             Resource =
                                                 Framework.SettingsHelper.PasswordVaultResourceName
                                         };

            SettingsHelper.UpdateCredentialsInVault(passwordCredential);
        }

        #endregion
    }
}