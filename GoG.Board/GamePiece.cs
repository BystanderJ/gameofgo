using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using GoG.Shared.Engine;

namespace GoG.Board
{
    public sealed class GamePiece : Button
    {
        // This constructor is included so Blend can instantiate the class.
		public GamePiece()
        {
			DefaultStyleKey = typeof(GamePiece);

            Sequence = "361";
            Color = GoColor.White;
            IsHint = true;
            IsLastMove = true;
            IsNewCapture = true;
            CircleMargin = new Thickness(2);

		    IsTabStop = false;

            Loaded += OnLoaded;
		}

	    public GamePiece(string postion, double circleMargin, string sequence, GoColor? color, bool isHint, bool isNewPiece, bool isNewCapture)
        {
            DefaultStyleKey = typeof(GamePiece);

	        Position = postion;
            Sequence = sequence;
            Color = color;
            IsHint = isHint;
            IsLastMove = isNewPiece;
            IsNewCapture = isNewCapture;
            CircleMargin = new Thickness(circleMargin);

            Loaded += OnLoaded;
        }

        public string Position { get; set; }

        // These properties are from the PieceState.
        public GoColor? Color { get; set; }
        public GoColor? Territory { get; set; }
        public bool IsHint { get; set; }
        public bool IsLastMove { get; set; }
        public bool IsNewCapture { get; set; }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            UpdateVisualStateMainProperties();
            UpdateVisualStateTerritory();
        }

        internal void UpdateVisualStateTerritory()
        {
            if (Territory == null)
                VisualStateManager.GoToState(this, "NoTerritory", true);
            else
                VisualStateManager.GoToState(this, Territory.Value + "Territory", true);
        }

        internal void UpdateVisualStateMainProperties()
        {
            if (Color == null)
            {
                if (IsHint)
                {
                    if (IsNewCapture)
                        VisualStateManager.GoToState(this, "HintNewCapture", true);
                    else
                        VisualStateManager.GoToState(this, "Hint", true);
                }
                else
                {
                    if (IsNewCapture)
                        VisualStateManager.GoToState(this, "NewCapture", true);
                    else
                        VisualStateManager.GoToState(this, "Blank", true);
                }
            }
            else
            {
                if (IsLastMove)
                    VisualStateManager.GoToState(this, Color + "LastMove", true);
                else
                    VisualStateManager.GoToState(this, Color.ToString(), true);
            }
        }

        #region Sequence
        public string Sequence
        {
            get => (string)GetValue(SequenceProperty); set => SetValue(SequenceProperty, value);
        }
        public static readonly DependencyProperty SequenceProperty =
            DependencyProperty.Register("Sequence", typeof(string), typeof(GamePiece), new PropertyMetadata(null));
        #endregion Sequence

        #region CircleMargin
        public Thickness CircleMargin
        {
            get => (Thickness)GetValue(CircleMarginProperty); set => SetValue(CircleMarginProperty, value);
        }
        public static readonly DependencyProperty CircleMarginProperty =
            DependencyProperty.Register("CircleMargin", typeof(Thickness), typeof(GamePiece), new PropertyMetadata(5D));
        #endregion CircleMargin
    }
}
