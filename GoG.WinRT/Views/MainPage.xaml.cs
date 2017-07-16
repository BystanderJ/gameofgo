using System;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace GoG.WinRT.Views
{
    public sealed partial class MainPage : NavigationAwarePage
    {
        public MainPage()
        {
            InitializeComponent();

            SizeChanged += OnSizeChanged;

            // Set the min size to 330 * 400
            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size { Width = 330, Height = 400 });
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs sizeChangedEventArgs)
        {
            if (sizeChangedEventArgs.NewSize.Width < 450)
            {
                MainStackPanel.Margin = new Thickness(30, 0, 0, 0);
                PageTitle.Style = Application.Current.Resources["SnappedPageHeaderTextStyle"] as Style;
            }
            else
            {
                MainStackPanel.Margin = new Thickness(120, 0, 0, 0);
                PageTitle.Style = Application.Current.Resources["PageHeaderTextStyle"] as Style;
            }

        }
    }
}
