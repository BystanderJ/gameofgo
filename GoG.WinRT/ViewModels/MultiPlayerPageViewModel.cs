using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using GoG.Infrastructure.Engine;
using GoG.Infrastructure.Services.Multiplayer;
using Prism.Commands;
using Prism.Windows.AppModel;
using GoG.WinRT.Services;
using Prism.Windows.Navigation;

// ReSharper disable RedundantCatchClause

namespace GoG.WinRT.ViewModels
{
    public class MultiPlayerPageViewModel : PageViewModel
    {
        #region Ctor
        public MultiPlayerPageViewModel(INavigationService navigationService,
            ISessionStateService sessionStateService,
            IGameEngine engine) : base(navigationService, sessionStateService, engine)
        {
            _boardEdgeSize = 9;
            _sizes = new List<Pair>
                {
                    new Pair("9x9 (Small)", 9),
                    new Pair("13x13 (Medium)", 13),
                    new Pair("19x19 (Full Size)", 19)
                };

            //_secondsPerTurn = 15;
            //_seconds = new List<Pair>
            //    {
            //        new Pair("No Limit", 0),
            //        new Pair("10 Seconds", 10),
            //        new Pair("15 Seconds", 15),
            //        new Pair("20 Seconds", 20),
            //        new Pair("30 Seconds", 30),
            //    };
            //for (int i = 3; i <= 180; i++)
            //    _seconds.Add(new Pair(i + " Seconds", i));

            _difficultyLevel = 6;
            _difficulties = new List<Pair>
                {
                    new Pair("1 (Novice)", 0),
                    new Pair("2", 1),
                    new Pair("3", 2),
                    new Pair("4 (Easy)", 3),
                    new Pair("5", 4),
                    new Pair("6", 5),
                    new Pair("7 (Normal)", 6),
                    new Pair("8", 7),
                    new Pair("9", 8),
                    new Pair("10 (Hard)", 9),
                };
            _color = (int)GoColor.Black;
            _colors = new List<Pair>
                {
                    new Pair("Black", (int)GoColor.Black),
                    new Pair("White", (int)GoColor.White)
                };
            _name = "";
            //_komi = (decimal)6.5;
        }
        #endregion Ctor

        #region Properties

        #region ActiveGame
        private Guid _activeGame;
        [RestorableState]
        public Guid ActiveGame
        {
            get => _activeGame;
            set
            {
                SetProperty(ref _activeGame, value);
                RaisePropertyChanged();
                // ReSharper disable once ExplicitCallerInfoArgument
                RaisePropertyChanged(nameof(IsActiveGame));
            }
        }
        #endregion ActiveGame

        #region IsActiveGame
        public bool IsActiveGame => _activeGame != Guid.Empty;
        #endregion IsActiveGame

        #region Name
        private string _name;
        [Required(ErrorMessage = "Name is required.")]
        public string Name
        {
            get => _name;
            set { SetProperty(ref _name, value); EnterLobbyCommand.RaiseCanExecuteChanged(); }
        }
        #endregion Name

        #region Sizes
        private List<Pair> _sizes;
        public List<Pair> Sizes
        {
            get => _sizes; set => SetProperty(ref _sizes, value);
        }
        #endregion Sizes

        #region Difficulties
        private List<Pair> _difficulties;
        public List<Pair> Difficulties
        {
            get => _difficulties; set => SetProperty(ref _difficulties, value);
        }
        #endregion Difficulties

        #region BoardEdgeSize
        private int _boardEdgeSize;
        public int BoardEdgeSize
        {
            get => _boardEdgeSize; set => SetProperty(ref _boardEdgeSize, value);
        }
        #endregion BoardEdgeSize

        #region DifficultyLevel
        private int _difficultyLevel;
        public int DifficultyLevel
        {
            get => _difficultyLevel; set => SetProperty(ref _difficultyLevel, value);
        }
        #endregion DifficultyLevel

        #region Colors
        private List<Pair> _colors;
        public List<Pair> Colors
        {
            get => _colors; set => SetProperty(ref _colors, value);
        }
        #endregion Colors

        #region Color
        private int _color;
        public int Color
        {
            get => _color; set => SetProperty(ref _color, value);
        }
        #endregion Color

        #endregion Properties

        #region Commands

        #region EnterLobbyCommand
        private DelegateCommand _enterLobbyCommand;
        public DelegateCommand EnterLobbyCommand => _enterLobbyCommand ?? (_enterLobbyCommand = new DelegateCommand(ExecuteEnterLobby, CanEnterLobby));
        public bool CanEnterLobby() => true;
        public async void ExecuteEnterLobby()
        {
            Name = Name.Trim();

            if (string.IsNullOrWhiteSpace(Name))
            {
                await DisplayMessage("Name", "Please enter your name.  Make it unique!");
                return;
            }



            var userPrefs = new UserPrefs()
            {
                Name = Name,
                PreferredBoardSize = BoardEdgeSize,
                SkillLevel = DifficultyLevel
            };
            
            NavigationService.Navigate("Lobby", userPrefs);
        }
        #endregion EnterLobbyCommand
        
        #endregion Commands

        #region Virtuals

        #endregion Virtuals

        #region Helpers
        

        #endregion Helpers
    }
}
