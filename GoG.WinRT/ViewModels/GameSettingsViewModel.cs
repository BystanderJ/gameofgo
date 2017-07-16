using Prism.Windows.Mvvm;

namespace GoG.WinRT.ViewModels
{
    public class GameSettingsViewModel : ViewModelBase
    {
        private bool _isLastMoveIndicatorShowing;
        /// <summary>
        /// Indicate whether the previous move is indicated on the board.
        /// </summary>
        public bool IsLastMoveIndicatorShowing
        {
            get => _isLastMoveIndicatorShowing; set => SetProperty(ref _isLastMoveIndicatorShowing, value);
        }

        private bool _areRecentCapturesShowing;
        /// <summary>
        /// Indicate the recent captures from the latest move.
        /// </summary>
        public bool AreRecentCapturesShowing
        {
            get => _areRecentCapturesShowing; set => SetProperty(ref _areRecentCapturesShowing, value);
        }
    }
}
