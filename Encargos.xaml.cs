using Microsoft.Maui.Controls;
using Microsoft.Maui;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mercader.Domain.Entities;
using Mercader.ViewModels;
using Mercader.Data.Interfaces;

namespace Mercader
{
    public partial class Encargos : ContentPage
    {
        private readonly IDataRepository _repository;
        private readonly EncargosViewModel _viewModel;
        private bool _isLoading = false;

        public Encargos(IDataRepository repository, EncargosViewModel viewModel)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            InitializeComponent();
            BindingContext = _viewModel;
            ConfigurarPagina();
        }

        private void ConfigurarPagina()
        {
            Title = "📋 Encargos";
            this.Opacity = 0;
            this.FadeTo(1, 300);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.CargarEncargosCommand.ExecuteAsync(null);
            EncargosCollectionView.ItemsSource = _viewModel.Encargos;
        }

        private async Task CargarEncargos()
        {
            await _viewModel.CargarEncargosCommand.ExecuteAsync(null);
            EncargosCollectionView.ItemsSource = _viewModel.Encargos;
        }

        /// <summary>
        /// Evita la navegación hacia atrás desde esta pantalla.
        /// </summary>
        /// <returns>Siempre retorna true para bloquear el botón físico de retroceso.</returns>
        protected override bool OnBackButtonPressed() => true;

        /// <summary>
        /// Maneja el evento de tap sobre un número de contacto.
        /// Abre un menú con opciones para llamar, enviar SMS o copiar el número.
        /// </summary>
        private async void OnContactTapped(object sender, EventArgs e)
        {
            if (_isLoading) return;

            try
            {
                var frame = sender as Frame;
                if (frame?.BindingContext is Encargo encargo)
                {
                    await frame.ScaleTo(0.9, 50);
                    await frame.ScaleTo(1, 50);

                    if (string.IsNullOrWhiteSpace(encargo.Contacto))
                    {
                        await DisplayAlert("⚠️ Sin número",
                            "No hay número de contacto registrado para este encargo.", "OK");
                        return;
                    }

                    string nombreCliente = encargo.Nombre ?? "Cliente desconocido";
                    await MostrarOpcionesContacto(encargo.Contacto, nombreCliente);
                }
            }
            catch (Exception ex)
            {
                await MostrarError("Error de contacto",
                    $"No se pudo abrir la aplicación de contacto: {ex.Message}");
            }
        }

        /// <summary>
        /// Muestra las diferentes opciones disponibles para contactar al cliente.
        /// </summary>
        private async Task MostrarOpcionesContacto(string telefono, string nombreCliente)
        {
            try
            {
                string telefonoLimpio = LimpiarNumeroTelefono(telefono);

                string accion = await DisplayActionSheet(
                    $"📞 Contactar a {nombreCliente}",
                    "Cancelar",
                    null,
                    "📞 Llamar",
                    "💬 Enviar SMS",
                    "📋 Copiar número"
                );

                switch (accion)
                {
                    case "📞 Llamar":
                        await RealizarLlamada(telefonoLimpio);
                        break;

                    case "💬 Enviar SMS":
                        await EnviarSMS(telefonoLimpio);
                        break;

                    case "📋 Copiar número":
                        await CopiarNumero(telefonoLimpio);
                        break;
                }
            }
            catch (Exception ex)
            {
                await MostrarError("Error de contacto", ex.Message);
            }
        }

        /// <summary>
        /// Inicia una llamada telefónica hacia el número indicado.
        /// </summary>
        private async Task RealizarLlamada(string telefono)
        {
            try
            {
                PhoneDialer.Open(telefono);
            }
            catch (ArgumentNullException)
            {
                await DisplayAlert("❌ Error", "Número de teléfono inválido", "OK");
            }
            catch (Exception ex)
            {
                await MostrarError("Error al llamar", $"No se pudo realizar la llamada: {ex.Message}");
            }
        }

        /// <summary>
        /// Envía un mensaje SMS al número proporcionado.
        /// </summary>
        private async Task EnviarSMS(string telefono)
        {
            try
            {
                var message = new SmsMessage("Hola! Te contacto por tu encargo.", telefono);
                await Sms.ComposeAsync(message);
            }
            catch (FeatureNotSupportedException)
            {
                await DisplayAlert("❌ No compatible",
                    "El envío de SMS no está disponible en este dispositivo", "OK");
            }
            catch (Exception ex)
            {
                await MostrarError("Error al enviar SMS", ex.Message);
            }
        }

        /// <summary>
        /// Copia un número telefónico al portapapeles del sistema.
        /// </summary>
        private async Task CopiarNumero(string telefono)
        {
            try
            {
                await Clipboard.SetTextAsync(telefono);
                await DisplayAlert("✅ Copiado",
                    $"El número {telefono} se copió al portapapeles", "OK");
            }
            catch (Exception ex)
            {
                await MostrarError("Error al copiar", ex.Message);
            }
        }

        /// <summary>
        /// Limpia un número telefónico eliminando caracteres no numéricos.
        /// </summary>
        private string LimpiarNumeroTelefono(string telefono)
        {
            if (string.IsNullOrWhiteSpace(telefono))
                return string.Empty;

            string limpio = telefono.Trim();
            if (limpio.StartsWith("+"))
                limpio = "+" + System.Text.RegularExpressions.Regex.Replace(limpio[1..], @"[^\d]", "");
            else
                limpio = System.Text.RegularExpressions.Regex.Replace(limpio, @"[^\d]", "");

            return limpio;
        }

        /// <summary>
        /// Maneja el tap sobre un encargo para navegar a la vista de detalles.
        /// </summary>
        private async void OnItemTapped(object sender, EventArgs e)
        {
            if (_isLoading) return;

            try
            {
                var frame = sender as Frame;
                if (frame?.BindingContext is Encargo encargo)
                {
                    await frame.ScaleTo(0.95, 100);
                    await frame.ScaleTo(1, 100);
await Navigation.PushAsync(new DetalleEncargo(encargo, _repository));
                }
            }
            catch (Exception ex)
            {
                await MostrarError("Error de navegación",
                    $"No se pudo abrir los detalles del encargo: {ex.Message}");
            }
        }

        /// <summary>
        /// Acción de edición al deslizar un encargo.
        /// </summary>
        private async void OnEditSwipeItemInvoked(object sender, EventArgs e)
        {
            if (_isLoading) return;

            try
            {
                if (sender is SwipeItem swipeItem && swipeItem.BindingContext is Encargo encargo)
                    await EditarEncargo(encargo);
            }
            catch (Exception ex)
            {
                await MostrarError("Error de edición", ex.Message);
            }
        }

        /// <summary>
        /// Acción de eliminación al deslizar un encargo.
        /// </summary>
        private async void OnDeleteSwipeItemInvoked(object sender, EventArgs e)
        {
            if (_isLoading) return;

            try
            {
                if (sender is SwipeItem swipeItem && swipeItem.BindingContext is Encargo encargo)
                    await EliminarEncargo(encargo);
            }
            catch (Exception ex)
            {
                await MostrarError("Error de eliminación", ex.Message);
            }
        }

        /// <summary>
        /// Acción para ver detalles al deslizar un encargo.
        /// </summary>
        private async void OnDetallesSwipeItemInvoked(object sender, EventArgs e)
        {
            if (_isLoading) return;

            try
            {
                if (sender is SwipeItem swipeItem && swipeItem.BindingContext is Encargo encargo)
                    await MostrarDetalles(encargo);
            }
            catch (Exception ex)
            {
                await MostrarError("Error al mostrar detalles", ex.Message);
            }
        }

        #region Métodos de Negocio

        /// <summary>
        /// Abre la página de edición del encargo seleccionado.
        /// </summary>
        private async Task EditarEncargo(Encargo encargo)
        {
            try
            {
                await Navigation.PushAsync(new EditarEncargoPage(encargo, _repository));
            }
            catch (Exception ex)
            {
                await MostrarError("Error al editar",
                    $"No se pudo abrir la página de edición: {ex.Message}");
            }
        }

        /// <summary>
        /// Elimina un encargo con confirmación del usuario.
        /// </summary>
        private async Task EliminarEncargo(Encargo encargo)
        {
            try
            {
                string mensaje = $"¿Estás seguro de eliminar este encargo?\n\n" +
                               $"👤 Cliente: {encargo.Nombre}\n" +
                               $"📞 Teléfono: {encargo.Contacto}\n" +
                               $"💰 Precio: ${encargo.Precio:F2}\n" +
                               $"📦 Cantidad: {encargo.Cantidad}\n" +
                               $"📅 Pedido: {encargo.Fecha:dd/MM/yyyy}\n" +
                               $"🚚 Entrega: {encargo.FechaEntrega:dd/MM/yyyy}\n\n" +
                               $"⚠️ Esta acción no se puede deshacer.";

                bool confirmar = await DisplayAlert("🗑️ Eliminar Encargo", mensaje, "Sí, eliminar", "Cancelar");

                if (confirmar)
                {
                    _isLoading = true;
                    await _viewModel.EliminarEncargoCommand.ExecuteAsync(encargo);
                    await DisplayAlert("✅ Éxito", "El encargo se eliminó correctamente", "OK");
                    await CargarEncargos();
                }
            }
            catch (Exception ex)
            {
                await MostrarError("Error al eliminar", $"No se pudo eliminar el encargo: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
            }
        }

        /// <summary>
        /// Muestra los detalles completos de un encargo.
        /// </summary>
        private async Task MostrarDetalles(Encargo encargo)
        {
            try
            {
                await Navigation.PushAsync(new DetalleEncargo(encargo, _repository));
            }
            catch (Exception ex)
            {
                await MostrarError("Error al mostrar detalles",
                    $"No se pudo abrir la página de detalles: {ex.Message}");
            }
        }

        /// <summary>
        /// Registra una venta a partir de un encargo existente.
        /// </summary>
        private async Task ConcretarVenta(Encargo encargo)
        {
            try
            {
                string mensaje = $"¿Confirmar la venta de este encargo?\n\n" +
                               $"👤 Cliente: {encargo.Nombre}\n" +
                               $"📋 Descripción: {encargo.Descripcion}\n" +
                               $"💰 Precio: ${encargo.Precio:F2}\n" +
                               $"📦 Cantidad: {encargo.Cantidad}\n\n" +
                               $"El encargo se eliminará y se registrará como venta.";

                bool confirmar = await DisplayAlert("✅ Concretar Venta", mensaje, "Sí, concretar", "Cancelar");

                if (confirmar)
                {
                    _isLoading = true;

                    await _viewModel.ConvertirEnVentaCommand.ExecuteAsync(encargo);

                    await DisplayAlert("✅ Venta Concretada",
                        "La venta se registró correctamente y el encargo fue eliminado", "OK");

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
        /// Muestra un mensaje de error uniforme y lo registra en consola.
        /// </summary>
        private async Task MostrarError(string titulo, string mensaje)
        {
            await DisplayAlert($"❌ {titulo}", mensaje, "OK");
            Console.WriteLine($"❌ {titulo}: {mensaje}");
        }

        /// <summary>
        /// Método público para refrescar la lista de encargos desde otra vista.
        /// </summary>
        public async Task RefrescarEncargos() => await CargarEncargos();

        #endregion
    }
}
