using CommunityToolkit.Mvvm.ComponentModel;

namespace Mercader.ViewModels
{
    public abstract partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _title = "";

        protected async Task ExecuteBusyAsync(Func<Task> action)
        {
            IsBusy = true;
            try
            {
                await action();
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}