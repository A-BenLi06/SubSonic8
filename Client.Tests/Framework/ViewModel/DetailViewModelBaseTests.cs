namespace Client.Tests.Framework.ViewModel
{
    using Client.Common.Models;
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
    using Subsonic8.Framework.ViewModel;

    [TestClass]
    public abstract class DetailViewModelBaseTests<TSubsonicModel, TViewModel> :
        CollectionViewModelBaseTests<TViewModel, string>
        where TViewModel : IDetailViewModel<TSubsonicModel>, new() where TSubsonicModel : ISubsonicModel
    {
    }
}