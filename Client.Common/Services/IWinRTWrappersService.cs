namespace Client.Common.Services
{
    using System;
    using System.Threading.Tasks;
    using Client.Common.Helpers;
    using Windows.ApplicationModel.DataTransfer;
    using Windows.Foundation;
    using Windows.Storage;

    public interface IWinRTWrappersService
    {
        #region Public Methods and Operators

        Task<IStorageFile> GetNewStorageFile();

        Task<T> LoadFromFile<T>(IStorageFile storageFile) where T : new();

        Task<IStorageFile> OpenStorageFile();

        void RegisterMediaControlHandler(IMediaControlHandler mediaControlHandler);

        Task SaveToFile<T>(IStorageFile storageFile, T @object);

        void RegisterShareRequestHandler(Action<DataRequest> requestHandler);

        void ShowShareUI();

        #endregion
    }
}