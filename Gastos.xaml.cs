using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mercader.Domain.Entities;

namespace Mercader
{
    public partial class Gastos : ContentPage
    {
        private readonly DataRepository _repo;
        private bool _isLoading = false;

        public Gastos(DataRepository repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            InitializeComponent();
            ConfigurarPagina();
        }

        /// <summary>
        /// Configuración inicial de la página
        /// </summary>
        private void ConfigurarPagina()
        {
            // Configurar el título de la página
            Title = "💸 Gastos";

            // Aplicar animación de entrada suave
            this.Opacity = 0;
            this.FadeTo(1, 300);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarGastos();
        }

        /// <summary>
        /// <returns>Carga los gastos desde la base de datos</returns>
        /// </summary>
private async Task CargarGastos()
        {
            var gastos = await _repo.GetGastosAsync();
           
            // Limpiar y forzar refresh
            GastosCollectionView.ItemsSource = null;
            await Task.Delay(10);
            
            // Verificar si hay datos
            if (gastos?.Count > 0)
                {
                    GastosCollectionView.ItemsSource = gastos;
                    Console.WriteLine($"✅ Se cargaron {gastos.Count} gastos correctamente");
                }
                else
                {
                    GastosCollectionView.ItemsSource = new List<Gasto>();
                    Console.WriteLine("ℹ️ No se encontraron gastos en la base de datos");
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
                    var gasto = frame.BindingContext as Gasto;

                    if (gasto != null)
                    {
                        // Efecto visual de selección
                        await frame.ScaleTo(0.95, 100);
                        await frame.ScaleTo(1, 100);

                        // Navegar a detalles
                        await Navigation.PushAsync(new DetalleGasto(gasto, _repo));
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
                await Navigation.PushAsync(new EditarGastoPage(gasto, _repo));
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
                               $"⚠️ Esta acción no se puede deshacer.";

                bool confirmar = await DisplayAlert("🗑️ Eliminar Gasto",
                    mensaje, "Sí, eliminar", "Cancelar");

                if (confirmar)
                {
                    _isLoading = true;

                    // Eliminar de la base de datos
                    await _repo.DeleteGastoAsync(gasto);

                    // Mostrar mensaje de éxito
                    await DisplayAlert("✅ Éxito",
                        "El gasto se eliminó correctamente", "OK");

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
            await DisplayAlert($"❌ {titulo}", mensaje, "OK");
            Console.WriteLine($"❌ {titulo}: {mensaje}");
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