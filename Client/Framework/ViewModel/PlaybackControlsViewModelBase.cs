namespace Subsonic8.Framework.ViewModel
{
    using Client.Common.EventAggregatorMessages;
    using Caliburn.Micro;

    public abstract class PlaybackControlsViewModelBase : ViewModelBase, IPlaybackControlsViewModel
    {
        #region Public Methods and Operators

        public virtual void Next()
        {
            EventAggregator.PublishOnUIThread(new JumpToNextMessage());
        }

        public virtual void PlayPause()
        {
            EventAggregator.PublishOnUIThread(new PlayPauseMessage());
        }

        public virtual void Previous()
        {
            EventAggregator.PublishOnUIThread(new JumpToPreviousMessage());
        }

        public virtual void Stop()
        {
            EventAggregator.PublishOnUIThread(new StopMessage());
        }

        #endregion

        #region Methods

        protected override void OnEventAggregatorSet()
        {
            base.OnEventAggregatorSet();
            EventAggregator.Subscribe(this);
        }

        #endregion
    }
}