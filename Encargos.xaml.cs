using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace Mercader
{
    public partial class Encargos : ContentPage
    {
        private bool _isLoading = false;

        public Encargos()
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
            Title = "📋 Encargos";

            // Aplicar animación de entrada suave
            this.Opacity = 0;
            this.FadeTo(1, 300);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarEncargosConAnimacion();
        }

        /// <summary>
        /// Carga los encargos con animación visual
        /// </summary>
        private async Task CargarEncargosConAnimacion()
        {
            if (_isLoading) return;

            _isLoading = true;

            try
            {
                

                // Cargar datos
                await CargarEncargos();

                
            }
            catch (Exception ex)
            {
                await MostrarError("Error al cargar encargos", ex.Message);
            }
            finally
            {
                await MostrarIndicadorCarga(false);
                _isLoading = false;
            }
        }

        /// <summary>
        /// Carga los encargos desde la base de datos
        /// </summary>
        private async Task CargarEncargos()
        {
            try
            {
                var encargos = await App.DataRepo.GetEncargosAsync();

                // Verificar si hay datos
                if (encargos?.Count > 0)
                {
                    EncargosCollectionView.ItemsSource = encargos;
                    Console.WriteLine($"✅ Se cargaron {encargos.Count} encargos correctamente");
                }
                else
                {
                    Console.WriteLine("ℹ️ No se encontraron encargos en la base de datos");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al cargar los encargos: {ex.Message}");
                throw; // Re-lanzar para manejo en nivel superior
            }
        }

        /// <summary>
        /// Muestra u oculta un indicador de carga
        /// </summary>
        private async Task MostrarIndicadorCarga(bool mostrar)
        {
            // Aquí podrías agregar un ActivityIndicator si lo deseas
            await Task.Delay(mostrar ? 100 : 200);
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
                    var encargo = frame.BindingContext as Encargo;

                    if (encargo != null)
                    {
                        // Efecto visual de selección
                        await frame.ScaleTo(0.95, 100);
                        await frame.ScaleTo(1, 100);

                        // Navegar a detalles
                        await Navigation.PushAsync(new DetalleEncargo(encargo));
                    }
                }
            }
            catch (Exception ex)
            {
                await MostrarError("Error de navegación",
                    $"No se pudo abrir los detalles del encargo: {ex.Message}");
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
                var encargo = swipeItem?.BindingContext as Encargo;

                if (encargo != null)
                {
                    await EditarEncargo(encargo);
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
                var encargo = swipeItem?.BindingContext as Encargo;

                if (encargo != null)
                {
                    await EliminarEncargo(encargo);
                }
            }
            catch (Exception ex)
            {
                await MostrarError("Error de eliminación", ex.Message);
            }
        }

        /// <summary>
        /// Maneja la acción de ver detalles desde el swipe
        /// </summary>
        private async void OnDetallesSwipeItemInvoked(object sender, EventArgs e)
        {
            if (_isLoading) return;

            try
            {
                var swipeItem = sender as SwipeItem;
                var encargo = swipeItem?.BindingContext as Encargo;

                if (encargo != null)
                {
                    await MostrarDetalles(encargo);
                }
            }
            catch (Exception ex)
            {
                await MostrarError("Error al mostrar detalles", ex.Message);
            }
        }

        #region Métodos de Negocio

        /// <summary>
        /// Navega a la página de edición de encargo
        /// </summary>
        private async Task EditarEncargo(Encargo encargo)
        {
            try
            {
                await Navigation.PushAsync(new EditarEncargoPage(encargo));
            }
            catch (Exception ex)
            {
                await MostrarError("Error al editar",
                    $"No se pudo abrir la página de edición: {ex.Message}");
            }
        }

        /// <summary>
        /// Elimina un encargo con confirmación del usuario
        /// </summary>
        private async Task EliminarEncargo(Encargo encargo)
        {
            try
            {
                // Confirmación mejorada con más información
                string mensaje = $"¿Estás seguro de eliminar este encargo?\n\n" +
                               $"👤 Cliente: {encargo.Nombre}\n" +
                               $"💰 Precio: ${encargo.Precio:F2}\n" +
                               $"📦 Cantidad: {encargo.Cantidad}\n" +
                               $"📅 Fecha de pedido: {encargo.Fecha:dd/MM/yyyy}\n" +
                               $"🚚 Fecha de entrega: {encargo.FechaEntrega:dd/MM/yyyy}\n\n" +
                               $"⚠️ Esta acción no se puede deshacer.";

                bool confirmar = await DisplayAlert("🗑️ Eliminar Encargo",
                    mensaje, "Sí, eliminar", "Cancelar");

                if (confirmar)
                {
                    _isLoading = true;

                    // Eliminar de la base de datos
                    await App.DataRepo.DeleteEncargoAsync(encargo);

                    // Mostrar mensaje de éxito
                    await DisplayAlert("✅ Éxito",
                        "El encargo se eliminó correctamente", "OK");

                    // Recargar la lista
                    await CargarEncargos();
                }
            }
            catch (Exception ex)
            {
                await MostrarError("Error al eliminar",
                    $"No se pudo eliminar el encargo: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
            }
        }

        /// <summary>
        /// Muestra los detalles del encargo
        /// </summary>
        private async Task MostrarDetalles(Encargo encargo)
        {
            try
            {
                await Navigation.PushAsync(new DetalleEncargo(encargo));
            }
            catch (Exception ex)
            {
                await MostrarError("Error al mostrar detalles",
                    $"No se pudo abrir la página de detalles: {ex.Message}");
            }
        }

        /// <summary>
        /// Concreta una venta a partir de un encargo
        /// </summary>
        private async Task ConcretarVenta(Encargo encargo)
        {
            try
            {
                // Confirmación mejorada
                string mensaje = $"¿Confirmar la venta de este encargo?\n\n" +
                               $"👤 Cliente: {encargo.Nombre}\n" +
                               $"📋 Descripción: {encargo.Descripcion}\n" +
                               $"💰 Precio: ${encargo.Precio:F2}\n" +
                               $"📦 Cantidad: {encargo.Cantidad}\n\n" +
                               $"El encargo se eliminará y se registrará como venta.";

                bool confirmar = await DisplayAlert("✅ Concretar Venta",
                    mensaje, "Sí, concretar", "Cancelar");

                if (confirmar)
                {
                    _isLoading = true;

                    // Crear la venta
                    var venta = new Ventas
                    {
                        Descripcion = encargo.Descripcion,
                        Precio = encargo.Precio,
                        Cantidad = encargo.Cantidad,
                        Fecha = DateTime.Now
                    };

                    // Guardar la venta y eliminar el encargo
                    await App.DataRepo.SaveVentasAsync(venta);
                    await App.DataRepo.DeleteEncargoAsync(encargo);

                    // Mostrar mensaje de éxito
                    await DisplayAlert("✅ Venta Concretada",
                        "La venta se registró correctamente y el encargo fue eliminado", "OK");

                    // Recargar la lista
                    await CargarEncargos();
                }
            }
            catch (Exception ex)
            {
                await MostrarError("Error al concretar venta",
                    $"No se pudo concretar la venta: {ex.Message}");
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
        /// Refresca la lista de encargos (método público para uso externo)
        /// </summary>
        public async Task RefrescarEncargos()
        {
            await CargarEncargosConAnimacion();
        }

        #endregion
    }
}