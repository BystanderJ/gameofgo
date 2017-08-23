
using GoG.Shared.Engine;
using Prism.Windows.Mvvm;

// ReSharper disable ExplicitCallerInfoArgument

namespace GoG.WinRT.ViewModels
{
    public class PlayerViewModel : ViewModelBase
    {
        public PlayerViewModel(GoPlayer p, GoColor color)
        {
            _color = color;
            _name = p.Name;
            _playerType = p.PlayerType;
            _level = p.Level;
            _komi = p.Komi;
        }

        #region MoveCount
        private int _moveCount;
        public int MoveCount
        {
            get => _moveCount; set { _moveCount = value; RaisePropertyChanged(); }
        }
        #endregion MoveCount

        private string _name;
        public string Name
        {
            get => _name; set => SetProperty(ref _name, value);
        }

        private PlayerType _playerType;
        public PlayerType PlayerType
        {
            get => _playerType; set => SetProperty(ref _playerType, value);
        }

        private int _level;
        public int Level
        {
            get => _level; set => SetProperty(ref _level, value);
        }

        private GoColor _color;
        public GoColor Color
        {
            get => _color; set => SetProperty(ref _color, value);
        }

        private decimal _komi;
        public decimal Komi
        {
            get => _komi; set
            {
                if (SetProperty(ref _komi, value))
                    RaisePropertyChanged(nameof(Score));
            }
        }

        private int _area;
        public int Area
        {
            get => _area; set
            {
                if (SetProperty(ref _area, value))
                    RaisePropertyChanged(nameof(Score));
            }
        }
        
        #region Captured
        private int _captured;
        public int Captured
        {
            get => _captured;
            set
            {
                if (SetProperty(ref _captured, value, nameof(Captured)))
                {
                    RaisePropertyChanged(nameof(Prisoners));
                    RaisePropertyChanged(nameof(Score));
                }
            }
        }
        #endregion Captured

        #region Dead
        private int _dead;
        public int Dead
        {
            get => _dead;
            set
            {
                if (SetProperty(ref _dead, value, nameof(Dead)))
                {
                    RaisePropertyChanged(nameof(Prisoners));
                    RaisePropertyChanged(nameof(Score));
                }
            }
        }
        #endregion Dead

        public decimal Score => _komi + Prisoners + _area;
        public int Prisoners => _captured + _dead;
    }
}