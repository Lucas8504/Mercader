using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mercader.Domain.Entities;
using Mercader.Data.Interfaces;

namespace Mercader.ViewModels
{
    public partial class GastosViewModel : ObservableObject
    {
        private readonly IDataRepository _repository;

        [ObservableProperty]
        private ObservableCollection<Gasto> _gastos = new();

        [ObservableProperty]
        private bool _isBusy = false;

        public GastosViewModel(IDataRepository repository)
        {
            _repository = repository;
        }

        [RelayCommand]
        public async Task CargarGastosAsync()
        {
            IsBusy = true;
            try
            {
                var lista = await _repository.GetGastosAsync();
                Gastos = new ObservableCollection<Gasto>(lista);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task EliminarGastoAsync(Gasto gasto)
        {
            if (gasto is null) return;
            
            IsBusy = true;
            try
            {
                await _repository.DeleteGastoAsync(gasto);
                Gastos.Remove(gasto);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}