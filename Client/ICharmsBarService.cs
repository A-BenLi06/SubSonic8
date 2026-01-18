
namespace Subsonic8.Services
{
    using System;
    using Windows.ApplicationModel.Search;
    using Windows.Foundation;
    using Windows.UI.ApplicationSettings;

    public interface ICharmsBarService
    {
        void RegisterSearchQueryHandler(TypedEventHandler<SearchPane, SearchPaneQuerySubmittedEventArgs> handler);

        void RegisterSettingsRequestedHandler(TypedEventHandler<SettingsPane, SettingsPaneCommandsRequestedEventArgs> handler);
    }
}
