using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mercader.Domain.Entities;
using Mercader.Data.Interfaces;

namespace Mercader.ViewModels
{
    public partial class EncargosViewModel : BaseViewModel
    {
        private readonly IDataRepository _repository;

        [ObservableProperty]
        private ObservableCollection<Encargo> _encargos = new();

        public EncargosViewModel(IDataRepository repository)
        {
            _repository = repository;
        }

        [RelayCommand]
        public async Task CargarEncargosAsync()
        {
            await ExecuteBusyAsync(async () =>
            {
                var lista = await _repository.GetEncargosAsync();
                Encargos = new ObservableCollection<Encargo>(lista);
            });
        }

        [RelayCommand]
        public async Task EliminarEncargoAsync(Encargo encargo)
        {
            if (encargo is null) return;

            await ExecuteBusyAsync(async () =>
            {
                await _repository.DeleteEncargoAsync(encargo);
                Encargos.Remove(encargo);
            });
        }

        [RelayCommand]
        public async Task ConvertirEnVentaAsync(Encargo encargo)
        {
            if (encargo is null) return;

            await ExecuteBusyAsync(async () =>
            {
                var venta = new Ventas
                {
                    Descripcion = $"Venta de: {encargo.Nombre} - {encargo.Descripcion}",
                    Precio = encargo.Precio,
                    Cantidad = encargo.Cantidad,
                    Fecha = DateTime.Now
                };

                await _repository.SaveVentasAsync(venta);
                await _repository.DeleteEncargoAsync(encargo);
                Encargos.Remove(encargo);
            });
        }
    }
}
