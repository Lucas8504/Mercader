using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace Mercader
{
    public partial class Venta : ContentPage
    {
        private bool _isLoading = false;

        public Venta()
        {
            InitializeComponent();
            ConfigurarPagina();
        }

        /// <summary>
        /// Configuración inicial de la página
        /// </summary>
        private void ConfigurarPagina()
        {
            // Configurar el título de la página
            Title = "📊 Ventas";

            // Aplicar animación de entrada suave
            this.Opacity = 0;
            this.FadeTo(1, 300);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarVentas();
        }

        /// <summary>
        /// Carga las ventas desde la base de datos
        /// </summary>
        private async Task CargarVentas()
        {
            try
            {
                var ventas = await App.DataRepo.GetVentasAsync();

                // Verificar si hay datos
                if (ventas?.Count > 0)
                {
                    VentasCollectionView.ItemsSource = ventas;
                    Console.WriteLine($"✅ Se cargaron {ventas.Count} ventas correctamente");
                }
                else
                {
                    Console.WriteLine("ℹ️ No se encontraron ventas en la base de datos");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al cargar las ventas: {ex.Message}");
                throw; // Re-lanzar para manejo en nivel superior
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
                        await Navigation.PushAsync(new DetalleVenta(venta));
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
                await Navigation.PushAsync(new EditarVentaPage(venta));
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
                               $"⚠️ Esta acción no se puede deshacer.";

                bool confirmar = await DisplayAlert("🗑️ Eliminar Venta",
                    mensaje, "Sí, eliminar", "Cancelar");

                if (confirmar)
                {
                    _isLoading = true;

                    // Eliminar de la base de datos
                    await App.DataRepo.DeleteVentaAsync(venta);

                    // Mostrar mensaje de éxito
                    await DisplayAlert("✅ Éxito",
                        "La venta se eliminó correctamente", "OK");

                    // Recargar la lista
                    await CargarVentas();
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
            await DisplayAlert($"❌ {titulo}", mensaje, "OK");
            Console.WriteLine($"❌ {titulo}: {mensaje}");
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