using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mercader.Domain.Entities;
using Mercader.Data.Interfaces;

namespace Mercader.ViewModels
{
    public partial class VentasViewModel : BaseViewModel
    {
        private readonly IDataRepository _repository;

        [ObservableProperty]
        private ObservableCollection<Ventas> _ventas = new();

        public VentasViewModel(IDataRepository repository)
        {
            _repository = repository;
        }

        [RelayCommand]
        public async Task CargarVentasAsync()
        {
            await ExecuteBusyAsync(async () =>
            {
                var lista = await _repository.GetVentasAsync();
                Ventas = new ObservableCollection<Ventas>(lista);
            });
        }

        [RelayCommand]
        public async Task EliminarVentaAsync(Ventas venta)
        {
            if (venta is null) return;

            await ExecuteBusyAsync(async () =>
            {
                await _repository.DeleteVentaAsync(venta);
                Ventas.Remove(venta);
            });
        }
    }
}
