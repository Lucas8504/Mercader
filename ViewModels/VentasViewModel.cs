using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mercader.Domain.Entities;
using Mercader.Data.Interfaces;

namespace Mercader.ViewModels
{
    public partial class VentasViewModel : ObservableObject
    {
        private readonly IDataRepository _repository;

        [ObservableProperty]
        private ObservableCollection<Ventas> _ventas = new();

        [ObservableProperty]
        private bool _isBusy = false;

        public VentasViewModel(IDataRepository repository)
        {
            _repository = repository;
        }

        [RelayCommand]
        public async Task CargarVentasAsync()
        {
            IsBusy = true;
            try
            {
                var lista = await _repository.GetVentasAsync();
                Ventas = new ObservableCollection<Ventas>(lista);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task EliminarVentaAsync(Ventas venta)
        {
            if (venta is null) return;
            
            IsBusy = true;
            try
            {
                await _repository.DeleteVentaAsync(venta);
                Ventas.Remove(venta);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
