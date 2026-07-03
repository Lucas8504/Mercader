using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mercader.Domain.Entities;
using Mercader.Data.Interfaces;

namespace Mercader.ViewModels
{
    public partial class GastosViewModel : BaseViewModel
    {
        private readonly IDataRepository _repository;

        [ObservableProperty]
        private ObservableCollection<Gasto> _gastos = new();

        public GastosViewModel(IDataRepository repository)
        {
            _repository = repository;
        }

        [RelayCommand]
        public async Task CargarGastosAsync()
        {
            await ExecuteBusyAsync(async () =>
            {
                var lista = await _repository.GetGastosAsync();
                Gastos = new ObservableCollection<Gasto>(lista);
            });
        }

        [RelayCommand]
        public async Task EliminarGastoAsync(Gasto gasto)
        {
            if (gasto is null) return;

            await ExecuteBusyAsync(async () =>
            {
                await _repository.DeleteGastoAsync(gasto);
                Gastos.Remove(gasto);
            });
        }
    }
}
