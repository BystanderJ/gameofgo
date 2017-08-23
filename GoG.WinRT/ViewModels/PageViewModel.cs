using Prism.Windows.Navigation;
using Prism.Commands;
using System.Collections.Generic;
using GoG.WinRT.Services;
using Prism.Windows.AppModel;

namespace GoG.WinRT.ViewModels
{
    public class PageViewModel : BaseViewModel
    {
        #region Data
        protected readonly INavigationService NavigationService;
        protected readonly ISessionStateService SessionStateService;
        protected readonly IGameEngine GameEngine;
        protected bool AbortOperation;
        #endregion Data

        #region Ctor
        public PageViewModel(INavigationService navigationService,
            ISessionStateService sessionStateService,
            IGameEngine gameEngine)
        {
            NavigationService = navigationService;
            SessionStateService = sessionStateService;
            GameEngine = gameEngine;
        }

        #endregion Ctor

        #region Commands
        
        #region GoBackCommand
        DelegateCommand _goBackCommand;
        public DelegateCommand GoBackCommand
        {
            get { if (_goBackCommand == null) _goBackCommand = new DelegateCommand(ExecuteGoBack, BaseCanGoBack); return _goBackCommand; }
        }
        private bool BaseCanGoBack()
        {
            return NavigationService.CanGoBack() && CanGoBack();
        }
        private void ExecuteGoBack()
        {
            NavigationService.GoBack();
        }
        #endregion GoBackCommand

        #endregion Commands

        #region Virtuals
        public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            base.OnNavigatingFrom(e, viewModelState, suspending);
            AbortOperation = true;
        }

        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            AbortOperation = false;
            base.OnNavigatedTo(e, viewModelState);
        }

        /// <summary>
        /// Override to place an additional restriction on the GoBackCommand.
        /// </summary>
        /// <returns></returns>
        protected virtual bool CanGoBack()
        {
            return true;
        }

        protected virtual void RaiseCommandsChanged()
        {
            GoBackCommand.RaiseCanExecuteChanged();
        }

        #endregion Virtuals

        #region Properties

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    RaisePropertyChanged();
                    OnIsBusyChanged();
                }
            }
        }

        protected virtual void OnIsBusyChanged()
        {
            
        }

        private string _busyMessage;
        public string BusyMessage
        {
            get => _busyMessage; set => SetProperty(ref _busyMessage, value);
        }
        #endregion Properties

        #region Helpers
        /// <summary>
        /// Goes back, but on the UI thread and after any current navigation action has completed.
        /// </summary>
        protected void GoBackDeferred()
        {
            // Note: Calling NavService.GoBack() in context of an existing navigation action causes
            // an exception.  Hence the need for this helper method.
            RunOnUiThread(() => NavigationService.GoBack());
        }
        #endregion Helpers
    }
}
