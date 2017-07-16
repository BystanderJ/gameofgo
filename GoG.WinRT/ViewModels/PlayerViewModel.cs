using GoG.Infrastructure.Engine;
using Prism.Windows.Mvvm;

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
            get => _moveCount; set { _moveCount = value; OnPropertyChanged("MoveCount"); }
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

        private decimal _score;
        public decimal Score => _komi + _prisoners + _area;

        private int _prisoners = 0;
        public int Prisoners
        {
            get => _prisoners; set
            {
                if (SetProperty(ref _prisoners, value))
                    RaisePropertyChanged(nameof(Score));
            }
        }
    }
}