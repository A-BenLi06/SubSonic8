
namespace Subsonic8.Services
{
    using Windows.ApplicationModel.Search;
    using Windows.Foundation;
    using Windows.UI.ApplicationSettings;

    public class CharmsBarService : ICharmsBarService
    {
        public void RegisterSearchQueryHandler(TypedEventHandler<SearchPane, SearchPaneQuerySubmittedEventArgs> handler)
        {
            SearchPane.GetForCurrentView().QuerySubmitted += handler;
        }

        public void RegisterSettingsRequestedHandler(TypedEventHandler<SettingsPane, SettingsPaneCommandsRequestedEventArgs> handler)
        {
            SettingsPane.GetForCurrentView().CommandsRequested += handler;
        }
    }
}
