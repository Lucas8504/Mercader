using CommunityToolkit.Mvvm.ComponentModel;

namespace Mercader.ViewModels
{
    public abstract partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _title = "";

        [ObservableProperty]
        private string? _errorMessage;

        /// <summary>
        /// Ejecuta una acción async con manejo de errores.
        /// Si falla, registra el error en ErrorMessage y en Debug output.
        /// No relanza la excepción — el error se maneja internamente.
        /// </summary>
        protected async Task ExecuteBusyAsync(Func<Task> action)
        {
            IsBusy = true;
            ErrorMessage = null;
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                System.Diagnostics.Debug.WriteLine($"[ERROR] {GetType().Name}: {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}