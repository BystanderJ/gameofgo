using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using GoG.Infrastructure.Engine;
using GoG.Infrastructure.Services.Engine;
using Prism.Commands;
using Prism.Windows.AppModel;
using GoG.WinRT.Services;
using Prism.Windows.Navigation;

// ReSharper disable RedundantCatchClause

namespace GoG.WinRT.ViewModels
{
    public class SinglePlayerPageViewModel : PageViewModel
    {
        #region Data
        private readonly IRepository _repository;
        #endregion Data

        #region Ctor
        public SinglePlayerPageViewModel(INavigationService navigationService,
            ISessionStateService sessionStateService, 
            IGameEngine engine, IRepository repository) : base(navigationService, sessionStateService, engine)
        {
            _repository = repository;
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
            _name = "Homer";
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

        //#region ActiveGameState
        //private GoGameState _activeGameState;
        //public GoGameState ActiveGameState
        //{
        //    get { return _activeGameState; }
        //    set { _activeGameState = value; OnPropertyChanged("ActiveGameState"); }
        //}
        //#endregion ActiveGameState

        private string _name;
        [RestorableState]
        [Required(ErrorMessage = "Name is required.")]
        public string Name
        {
            get => _name;
            set { SetProperty(ref _name, value); PlayCommand.RaiseCanExecuteChanged(); }
        }

        private List<Pair> _sizes;
        public List<Pair> Sizes
        {
            get => _sizes; set => SetProperty(ref _sizes, value);
        }

        //#region Seconds
        //private List<Pair> _seconds;
        //public List<Pair> Seconds
        //{
        //    get { return _seconds; }
        //    set { _seconds = value; OnPropertyChanged("Seconds"); }
        //}
        //#endregion Seconds

        private List<Pair> _difficulties;
        public List<Pair> Difficulties
        {
            get => _difficulties; set => SetProperty(ref _difficulties, value);
        }

        private int _boardEdgeSize;
        [RestorableState]
        public int BoardEdgeSize
        {
            get => _boardEdgeSize; set => SetProperty(ref _boardEdgeSize, value);
        }

        private int _difficultyLevel;
        [RestorableState]
        public int DifficultyLevel
        {
            get => _difficultyLevel; set => SetProperty(ref _difficultyLevel, value);
        }

        private List<Pair> _colors;
        public List<Pair> Colors
        {
            get => _colors; set => SetProperty(ref _colors, value);
        }

        private int _color;
        [RestorableState]
        public int Color
        {
            get => _color; set => SetProperty(ref _color, value);
        }
        
        #endregion Properties

        #region Commands
        
        #region PlayCommand
        private DelegateCommand _playCommand;
        public DelegateCommand PlayCommand => _playCommand ?? (_playCommand = new DelegateCommand(ExecutePlay, CanPlay));
        public bool CanPlay() => !string.IsNullOrWhiteSpace(Name);
        public void ExecutePlay()
        {
            // Start a new game by calling server, then go to game page.
            StartNewGame();
        }
        #endregion PlayCommand
       
        #region ResumeCommand
        private DelegateCommand _resumeCommand;
        public DelegateCommand ResumeCommand => _resumeCommand ?? (_resumeCommand = new DelegateCommand(ExecuteResume, CanResume));
        public bool CanResume()
        {
            return true;
        }
        public void ExecuteResume()
        {
            NavigationService.Navigate("Game", ActiveGame);
        }
        #endregion ResumeCommand

        #endregion Commands

        #region Virtuals
        
        #endregion Virtuals

        #region Helpers

        private async void StartNewGame()
        {
            try
            {
                var success = false;
                GoResponse resp = null;

                for (int tries = 0; !AbortOperation && !success && tries < 5; tries++)
                {
                    BusyMessage = "Starting game...";
                    IsBusy = true;

                    // Create game from user's selections.
                    var p1 = new GoPlayer();
                    var p2 = new GoPlayer()
                    {
                        Komi = 6.5M
                    };
                    if (Color == (int)GoColor.Black)
                    {
                        p1.Name = Name;
                        p1.PlayerType = PlayerType.Human;

                        p2.Name = "Fuego";
                        p2.PlayerType = PlayerType.AI;
                        p2.Level = DifficultyLevel;
                    }
                    else
                    {
                        p2.Name = Name;
                        p2.PlayerType = PlayerType.Human;

                        p1.Name = "Fuego";
                        p1.PlayerType = PlayerType.AI;
                        p1.Level = DifficultyLevel;
                    }
                    var tmpState = new GoGame(
                        (byte)BoardEdgeSize,
                        p1, p2,
                        GoGameStatus.Active,
                        GoColor.Black,
                        "",
                        "",
                        new List<GoMoveHistoryItem>(), 
                        0);
                    tmpState.Id = Guid.NewGuid();
                    resp = await GameEngine.CreateGameAsync(tmpState);
                    BusyMessage = null;
                    IsBusy = false;

                    if (resp.ResultCode == GoResultCode.Success)
                    {
                        if (ActiveGame != Guid.Empty)
                            await GameEngine.DeleteGameAsync(ActiveGame);

                        success = true;
                        ActiveGame = tmpState.Id;
                    }
                }

                if (AbortOperation)
                    return;

                if (success)
                    NavigationService.Navigate("Game", ActiveGame);
                else
                {
                    if (resp != null)
                        await DisplayErrorCode(resp.ResultCode);
                    else
                        await DisplayErrorCode(GoResultCode.InternalError);
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                BusyMessage = null;
                IsBusy = false;
            }
        }
        
        #endregion Helpers
    }

    public class Pair
    {
        public Pair(string desc, int value)
        {
            Desc = desc;
            Value = value;
        }

        public string Desc { get; set; }
        public int Value { get; set; }
    }
}
