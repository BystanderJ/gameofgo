﻿using Windows.UI.Xaml;
using GoG.Shared.Engine;

namespace GoG.WinRT.Views
{
    public sealed partial class HistoryItemUserControl
    {
        public HistoryItemUserControl()
        {
            InitializeComponent();

            DataContextChanged += HistoryItemUserControl_DataContextChanged;
        }

        private void HistoryItemUserControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs e)
        {
            if (e.NewValue == null)
                return;

            var hi = (GoMoveHistoryItem)e.NewValue;

            BlackEllipse.Visibility = hi.Move.Color == GoColor.Black ? Visibility.Visible : Visibility.Collapsed;
            WhiteEllipse.Visibility = hi.Move.Color == GoColor.White ? Visibility.Visible : Visibility.Collapsed;

            switch (hi.Move.MoveType)
            {
                case MoveType.Normal:
                    MoveTextBlock.Text = hi.Move.Position;
                    break;
                case MoveType.Pass:
                    MoveTextBlock.Text = "Pass";
                    break;
                case MoveType.Resign:
                    MoveTextBlock.Text = "Resign";
                    break;
            }            
        }

    }
}
