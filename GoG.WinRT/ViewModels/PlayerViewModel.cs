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
            get { return _moveCount; }
            set { _moveCount = value; OnPropertyChanged("MoveCount"); }
        }
        #endregion MoveCount

        private string _name;
        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }

        private PlayerType _playerType;
        public PlayerType PlayerType
        {
            get { return _playerType; }
            set { SetProperty(ref _playerType, value); }
        }

        private int _level;
        public int Level
        {
            get { return _level; }
            set { SetProperty(ref _level, value); }
        }

        private GoColor _color;
        public GoColor Color
        {
            get { return _color; }
            set { SetProperty(ref _color, value); }
        }

        private decimal _komi;
        public decimal Komi
        {
            get { return _komi; }
            set
            {
                if (SetProperty(ref _komi, value))
                    RaisePropertyChanged(nameof(Score));
            }
        }

        private int _area;
        public int Area
        {
            get { return _area; }
            set
            {
                if (SetProperty(ref _area, value))
                    RaisePropertyChanged(nameof(Score));
            }
        }

        private decimal _score;
        public decimal Score
        {
            get { return _komi + _prisoners + _area; }
        }

        private int _prisoners = 0;
        public int Prisoners
        {
            get { return _prisoners; }
            set
            {
                if (SetProperty(ref _prisoners, value))
                    RaisePropertyChanged(nameof(Score));
            }
        }
    }
}