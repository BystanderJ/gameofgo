using System;
using GoG.Shared.Engine;
using Prism.Windows.Mvvm;

namespace GoG.Board
{
    public class PieceStateViewModel : ViewModelBase
    {
        public PieceStateViewModel(string position, string sequence, GoColor? color,
            bool isHint, bool isNewPiece, bool isNewCapture)
        {
            Position = position;
            _sequence = sequence;
            _color = color;
            _isHint = isHint;
            _isNewPiece = isNewPiece;
            _isNewCapture = isNewCapture;
        }

        public event EventHandler MultiplePropertiesChanged;
        public event EventHandler TerritoryChanged;

        public string Position { get; }

        private string _sequence;
        public string Sequence
        {
            get => _sequence; set => SetProperty(ref _sequence, value);
        }

        private GoColor? _color;
        public GoColor? Color
        {
            get => _color; set => SetProperty(ref _color, value);
        }

        private GoColor? _territory;
        public GoColor? Territory
        {
            get => _territory;
            set
            {
                if (SetProperty(ref _territory, value))
                    TerritoryChanged?.Invoke(this, null);
            }
        }

        private bool _isHint;
        public bool IsHint
        {
            get => _isHint; set => SetProperty(ref _isHint, value);
        }

        private bool _isNewPiece;
        public bool IsNewPiece
        {
            get => _isNewPiece; set => SetProperty(ref _isNewPiece, value);
        }

        private bool _isNewCapture;
        public bool IsNewCapture
        {
            get => _isNewCapture; set => SetProperty(ref _isNewCapture, value);
        }

        public void RaiseMultiplePropertiesChanged()
        {
            var e = MultiplePropertiesChanged;
            e?.Invoke(this, null);
        }
    }
}