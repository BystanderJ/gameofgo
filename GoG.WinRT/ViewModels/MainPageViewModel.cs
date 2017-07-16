using System;
using Windows.System;
using Prism.Commands;
using Prism.Windows.AppModel;
using GoG.WinRT.Services;
using Prism.Windows.Navigation;

namespace GoG.WinRT.ViewModels
{
    public class MainPageViewModel : PageViewModel
    {
        #region Ctor
        public MainPageViewModel(INavigationService navigationService,
            ISessionStateService sessionStateService, 
            IGameEngine engine) : base(navigationService, sessionStateService, engine)
        {
            
        }
        #endregion Ctor
        
        #region Commands

        #region LaunchUrlCommand
        DelegateCommand<string> _launchUrlCommand;
        public DelegateCommand<string> LaunchUrlCommand => _launchUrlCommand ?? (_launchUrlCommand = new DelegateCommand<string>(ExecuteLaunchUrl, CanLaunchUrl));
        public bool CanLaunchUrl(string url) => true;
        public async void ExecuteLaunchUrl(string url)
        {
            // Launch the URI
            await Launcher.LaunchUriAsync(new Uri(url));
        }
        #endregion LaunchUrlCommand


        #region SinglePlayerCommand
        private DelegateCommand _singlePlayerCommand;
        public DelegateCommand SinglePlayerCommand => _singlePlayerCommand ?? (_singlePlayerCommand = new DelegateCommand(ExecuteSinglePlayerCommand, CanSinglePlayerCommand));
        public bool CanSinglePlayerCommand() => true;
        public void ExecuteSinglePlayerCommand()
        {
            NavigationService.Navigate("SinglePlayer", null);
        }
        #endregion SinglePlayerCommand


        #region MultiPlayerCommand
        private DelegateCommand _multiPlayerCommand;
        public DelegateCommand MultiPlayerCommand => _multiPlayerCommand ?? (_multiPlayerCommand = new DelegateCommand(ExecuteMultiPlayerCommand, CanMultiPlayerCommand));
        public bool CanMultiPlayerCommand() => true;
        public void ExecuteMultiPlayerCommand()
        {
            NavigationService.Navigate("MultiPlayer", null);
        }
        #endregion MultiPlayerCommand


        #endregion Commands
    }
}
