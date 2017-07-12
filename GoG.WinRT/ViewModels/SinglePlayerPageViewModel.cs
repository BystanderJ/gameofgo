﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Windows.System;
using Microsoft.Practices.Unity;
using GoG.Infrastructure.Engine;
using GoG.Infrastructure.Services.Engine;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Windows.AppModel;
using GoG.WinRT.Services;
using System.Threading.Tasks;

namespace GoG.WinRT.ViewModels
{
    public class SinglePlayerPageViewModel : PageViewModel
    {
        #region Ctor
        public SinglePlayerPageViewModel(IUnityContainer c) : base(c)
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
            _name = "Homer";
            //_komi = (decimal)4.5;
            
        }
        #endregion Ctor

        #region Properties

        #region ActiveGame
        private IGame _activeGame;
        public IGame ActiveGame
        {
            get { return _activeGame; }
            set
            {
                SetProperty(ref _activeGame, value);
                RaisePropertyChanged();
            }
        }
        #endregion ActiveGame

        #region IsActiveGame
        public bool IsActiveGame
        {
            get { return _activeGame != null; }
        }
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
            get { return _name; }
            set { SetProperty(ref _name, value); PlayCommand.RaiseCanExecuteChanged(); }
        }

        private List<Pair> _sizes;
        public List<Pair> Sizes
        {
            get { return _sizes; }
            set { SetProperty(ref _sizes, value); }
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
            get { return _difficulties; }
            set { SetProperty(ref _difficulties, value); }
        }

        private int _boardEdgeSize;
        [RestorableState]
        public int BoardEdgeSize
        {
            get { return _boardEdgeSize; }
            set { SetProperty(ref _boardEdgeSize, value); }
        }

        private int _difficultyLevel;
        [RestorableState]
        public int DifficultyLevel
        {
            get { return _difficultyLevel; }
            set { SetProperty(ref _difficultyLevel, value); }
        }

        private List<Pair> _colors;
        public List<Pair> Colors
        {
            get { return _colors; }
            set { SetProperty(ref _colors, value); }
        }

        private int _color;
        [RestorableState]
        public int Color
        {
            get { return _color; }
            set { SetProperty(ref _color, value); }
        }

        //private decimal _komi;
        //[RestorableState]
        //public decimal Komi
        //{
        //    get { return _komi; }
        //    set { SetProperty(ref _komi, value); }
        //}

        //#region SecondsPerTurn
        //private int _secondsPerTurn;
        //[RestorableState]
        //public int SecondsPerTurn
        //{
        //    get { return _secondsPerTurn; }
        //    set { _secondsPerTurn = value; OnPropertyChanged("SecondsPerTurn"); }
        //}
        //#endregion SecondsPerTurn

        #endregion Properties

        #region Commands

        #region LaunchUrlCommand
        DelegateCommand<string> _launchUrlCommand;
        public DelegateCommand<string> LaunchUrlCommand => _launchUrlCommand ?? (_launchUrlCommand = new DelegateCommand<string>(ExecuteLaunchUrl, CanLaunchUrl));
        public bool CanLaunchUrl(string url) => true;
        public void ExecuteLaunchUrl(string url)
        {
            // Launch the URI
            Launcher.LaunchUriAsync(new Uri(url));
        }
        #endregion LaunchUrlCommand
        
        #region PlayCommand
        DelegateCommand _playCommand;
        public DelegateCommand PlayCommand => _playCommand ?? (_playCommand = new DelegateCommand(ExecutePlay, CanPlay));
        public bool CanPlay() => !string.IsNullOrWhiteSpace(Name);
        public void ExecutePlay()
        {
            // Start a new game by calling server, then go to game page.
            StartNewGame();
        }
        #endregion PlayCommand
       
        #region ResumeCommand
        DelegateCommand _resumeCommand;
        public DelegateCommand ResumeCommand
        {
            get { return _resumeCommand ?? (_resumeCommand = new DelegateCommand(ExecuteResume, CanResume)); }
        }
        public bool CanResume()
        {
            return true;
        }
        public void ExecuteResume()
        {
            NavService.Navigate("Game", ActiveGame);
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
                bool success = false;
                GoGameStateResponse resp = null;

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
                    var tmpState = new GoGameState(
                        (byte)BoardEdgeSize,
                        p1, p2,
                        GoGameStatus.Active,
                        GoColor.Black,
                        "",
                        "",
                        new List<GoMoveHistoryItem>(), 
                        0);
                    ActiveGame = Container.Resolve<FuegoGame>();
                    await ActiveGame.StartAsync(tmpState);
                    BusyMessage = null;
                    IsBusy = false;

                    success = true;
                }

                if (AbortOperation)
                    return;

                if (success)
                    NavService.Navigate("Game", ActiveGame);
                else
                {
                    if (resp != null)
                        await DisplayErrorCode(resp.ResultCode);
                }
            }
            catch (Exception ex)
            {

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
