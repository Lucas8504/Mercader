using CommunityToolkit.Mvvm.ComponentModel;

namespace Mercader.ViewModels
{
    /// <summary>
    /// BaseViewModel usando CommunityToolkit.Mvvm.
    /// Provee INotifyPropertyChanged y [ObservableProperty] automaticamente.
    /// </summary>
    public abstract class BaseViewModel : ObservableObject
    {
        // ObservableObject ya provee:
        // - SetProperty<T>(ref T field, T value) heredado
        // - OnPropertyChanged(string?) heredado
        // - SetProperty<T>(T oldValue, T newValue, Action<T> callback)
    }
}
