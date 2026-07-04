using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mercader.Domain.Entities;
using Mercader.ViewModels;
using Mercader.Data.Interfaces;

namespace Mercader
{
    public partial class Venta : ContentPage
    {
        private readonly IDataRepository _repository;
        private readonly VentasViewModel _viewModel;
        private bool _isLoading = false;

        public Venta(IDataRepository repository, VentasViewModel viewModel)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            InitializeComponent();
            BindingContext = _viewModel;
            ConfigurarPagina();
        }

        private void ConfigurarPagina()
        {
            Title = "📊 Ventas";
            this.Opacity = 0;
            this.FadeTo(1, 300);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.CargarVentasCommand.ExecuteAsync(null);
            VentasCollectionView.ItemsSource = _viewModel.Ventas;
        }

        private async Task CargarVentas()
        {
            try
            {
                await _viewModel.CargarVentasCommand.ExecuteAsync(null);
                VentasCollectionView.ItemsSource = _viewModel.Ventas;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al cargar las ventas: {ex.Message}");
            }
        }

        protected override bool OnBackButtonPressed()
        {
            // Prevenir navegación hacia atrás
            return true;
        }

        /// <summary>
        /// Maneja el tap en un item para navegar a los detalles
        /// </summary>
        private async void OnItemTapped(object sender, EventArgs e)
        {
            if (_isLoading) return;

            try
            {
                var frame = sender as Frame;

                // Verificar que el frame no sea nulo antes de usarlo
                if (frame != null)
                {
                    var venta = frame.BindingContext as Ventas;

                    if (venta != null)
                    {
                        // Efecto visual de selección
                        await frame.ScaleTo(0.95, 100);
                        await frame.ScaleTo(1, 100);

                        // Navegar a detalles
                        await Navigation.PushAsync(new DetalleVenta(venta, _repository));
                    }
                }
            }
            catch (Exception ex)
            {
                await MostrarError("Error de navegación",
                    $"No se pudo abrir los detalles de la venta: {ex.Message}");
            }
        }

        /// <summary>
        /// Maneja la acción de editar desde el swipe
        /// </summary>
        private async void OnEditSwipeItemInvoked(object sender, EventArgs e)
        {
            if (_isLoading) return;

            try
            {
                var swipeItem = sender as SwipeItem;
                var venta = swipeItem?.BindingContext as Ventas;

                if (venta != null)
                {
                    await EditarVenta(venta);
                }
            }
            catch (Exception ex)
            {
                await MostrarError("Error de edición", ex.Message);
            }
        }

        /// <summary>
        /// Maneja la acción de eliminar desde el swipe
        /// </summary>
        private async void OnDeleteSwipeItemInvoked(object sender, EventArgs e)
        {
            if (_isLoading) return;

            try
            {
                var swipeItem = sender as SwipeItem;
                var venta = swipeItem?.BindingContext as Ventas;

                if (venta != null)
                {
                    await EliminarVenta(venta);
                }
            }
            catch (Exception ex)
            {
                await MostrarError("Error de eliminación", ex.Message);
            }
        }

       

        #region Métodos de Negocio

        /// <summary>
        /// Navega a la página de edición de venta
        /// </summary>
        private async Task EditarVenta(Ventas venta)
        {
            try
            {
                await Navigation.PushAsync(new EditarVentaPage(venta, _repository));
            }
            catch (Exception ex)
            {
                await MostrarError("Error al editar",
                    $"No se pudo abrir la página de edición: {ex.Message}");
            }
        }

        /// <summary>
        /// Elimina una venta con confirmación del usuario
        /// </summary>
        private async Task EliminarVenta(Ventas venta)
        {
            try
            {
                // Confirmación mejorada con más información
                string mensaje = $"¿Estás seguro de eliminar esta venta?\n\n" +
                               $"📋 Producto: {venta.Descripcion}\n" +
                               $"💰 Precio: ${venta.Precio:F2}\n" +
                               $"📦 Cantidad: {venta.Cantidad}\n" +
                               $"📅 Fecha: {venta.Fecha:dd/MM/yyyy}\n\n" +
                               "Esta accion no se puede deshacer.";

                bool confirmar = await DisplayAlert("Eliminar Venta",
                    mensaje, "Si, eliminar", "Cancelar");

                if (confirmar)
                {
                    _isLoading = true;

                    // Debug
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] EliminarVenta: Id={venta.Id}, Desc={venta.Descripcion}");

                    // Eliminar via ViewModel
                    await _viewModel.EliminarVentaCommand.ExecuteAsync(venta);

                    // Mostrar mensaje de éxito
                    await DisplayAlert("Exito",
                        "La venta se elimino correctamente", "OK");

                    // Debug - recargar y verificar
                    await CargarVentas();
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Después de recargar");
                }
            }
            catch (Exception ex)
            {
                await MostrarError("Error al eliminar",
                    $"No se pudo eliminar la venta: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
            }
        }

        #endregion

        #region Métodos de Utilidad

        /// <summary>
        /// Muestra un mensaje de error consistente
        /// </summary>
        private async Task MostrarError(string titulo, string mensaje)
        {
            await DisplayAlert(titulo, mensaje, "OK");
            Console.WriteLine($"{titulo}: {mensaje}");
        }

        /// <summary>
        /// Refresca la lista de ventas (método público para uso externo)
        /// </summary>
        public async Task RefrescarVentas()
        {
            await CargarVentas();
        }

        #endregion
    }
}