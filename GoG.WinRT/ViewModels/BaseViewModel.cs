using System;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Popups;
using GoG.Shared;
using GoG.Shared.Engine;
using Prism.Windows.Mvvm;

namespace GoG.WinRT.ViewModels
{
    public class BaseViewModel : ViewModelBase
    {
        #region Helpers

        protected static readonly CoreDispatcher Dispatcher;

        static BaseViewModel()
        {
            Dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
        }

        protected void RunOnUiThread(Action a)
        {
            Dispatcher?.RunAsync(CoreDispatcherPriority.Low, () => a());
        }

        protected static async Task DisplayMessage(string title, string msg)
        {
            var dialog = new MessageDialog(msg, title);
            await dialog.ShowAsync();
        }

        protected static async Task<string> DisplayQuery(string title, string msg, params string[] buttons)
        {
            var dialog = new MessageDialog(msg, title);
            for (var i = 0; i < buttons.Length; i++)
                dialog.Commands.Add(new UICommand(buttons[i], null, i));
            var c = await dialog.ShowAsync();
            return c.Label;
        }

        protected static async Task DisplayErrorCode(GoResultCode code)
        {
            var msg = EngineHelpers.GetResultCodeFriendlyMessage(code);
            await DisplayMessage("Whoops", msg);
        }

        #endregion Helpers
    }
}


