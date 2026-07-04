using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mercader.Domain.Entities;
using Mercader.ViewModels;
using Mercader.Data.Interfaces;

namespace Mercader
{
    public partial class Gastos : ContentPage
    {
        private readonly IDataRepository _repository;
        private readonly GastosViewModel _viewModel;
        private bool _isLoading = false;

        public Gastos(IDataRepository repository, GastosViewModel viewModel)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            InitializeComponent();
            BindingContext = _viewModel;
            ConfigurarPagina();
        }

        private void ConfigurarPagina()
        {
            Title = "💸 Gastos";
            this.Opacity = 0;
            this.FadeTo(1, 300);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.CargarGastosCommand.ExecuteAsync(null);
            GastosCollectionView.ItemsSource = _viewModel.Gastos;
        }

        private async Task CargarGastos()
        {
            await _viewModel.CargarGastosCommand.ExecuteAsync(null);
            GastosCollectionView.ItemsSource = _viewModel.Gastos;
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
                    var gasto = frame.BindingContext as Gasto;

                    if (gasto != null)
                    {
                        // Efecto visual de selección
                        await frame.ScaleTo(0.95, 100);
                        await frame.ScaleTo(1, 100);

                        // Navegar a detalles
                        await Navigation.PushAsync(new DetalleGasto(gasto, _repository));
                    }
                }
            }
            catch (Exception ex)
            {
                await MostrarError("Error de navegación",
                    $"No se pudo abrir los detalles del gasto: {ex.Message}");
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
                var gasto = swipeItem?.BindingContext as Gasto;

                if (gasto != null)
                {
                    await EditarGasto(gasto);
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
                var gasto = swipeItem?.BindingContext as Gasto;

                if (gasto != null)
                {
                    await EliminarGasto(gasto);
                }
            }
            catch (Exception ex)
            {
                await MostrarError("Error de eliminación", ex.Message);
            }
        }

        #region Métodos de Negocio

        /// <summary>
        /// Navega a la página de edición de gasto
        /// </summary>
        private async Task EditarGasto(Gasto gasto)
        {
            try
            {
                await Navigation.PushAsync(new EditarGastoPage(gasto, _repository));
            }
            catch (Exception ex)
            {
                await MostrarError("Error al editar",
                    $"No se pudo abrir la página de edición: {ex.Message}");
            }
        }

        /// <summary>
        /// Elimina un gasto con confirmación del usuario
        /// </summary>
        private async Task EliminarGasto(Gasto gasto)
        {
            try
            {
                // Confirmación mejorada con más información
                string mensaje = $"¿Estás seguro de eliminar este gasto?\n\n" +
                               $"💸 Descripción: {gasto.Descripcion}\n" +
                               $"💰 Monto: ${gasto.Monto:F2}\n" +
                               $"📦 Cantidad: {gasto.Cantidad}\n" +
                               $"📅 Fecha: {gasto.Fecha:dd/MM/yyyy}\n\n" +
                               "Esta accion no se puede deshacer.";

                bool confirmar = await DisplayAlert("Eliminar Gasto",
                    mensaje, "Si, eliminar", "Cancelar");

                if (confirmar)
                {
                    _isLoading = true;

                    // Eliminar via ViewModel
                    await _viewModel.EliminarGastoCommand.ExecuteAsync(gasto);

                    // Mostrar mensaje de éxito
                    await DisplayAlert("Exito",
                        "El gasto se elimino correctamente", "OK");

                    // Recargar la lista
                    await CargarGastos();
                }
            }
            catch (Exception ex)
            {
                await MostrarError("Error al eliminar",
                    $"No se pudo eliminar el gasto: {ex.Message}");
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
        /// Refresca la lista de gastos (método público para uso externo)
        /// </summary>
        public async Task RefrescarGastos()
        {
            await CargarGastos();
        }

        #endregion
    }
}