using Microsoft.Maui.Controls;
using Microsoft.Maui;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mercader.Domain.Entities;
using Mercader.ViewModels;
using Mercader.Core.ViewModels;
using Mercader.Data.Interfaces;
using Mercader.Services.Interfaces;
using Mercader.Core.Services.Localization;

namespace Mercader
{
    public partial class Encargos : ContentPage
    {
        private readonly IDataRepository _repository;
        private readonly IImageStorageService _imageStorageService;
        private readonly IServiceProvider _services;
        private readonly EncargosViewModel _viewModel;
        private bool _isLoading = false;

        public Encargos(IDataRepository repository, IImageStorageService imageStorageService, IServiceProvider services, EncargosViewModel viewModel)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _imageStorageService = imageStorageService ?? throw new ArgumentNullException(nameof(imageStorageService));
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            InitializeComponent();
            BindingContext = _viewModel;
            ConfigurarPagina();
        }

        private void ConfigurarPagina()
        {
            Title = Loc.Get("TituloEncargos");
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

        protected override bool OnBackButtonPressed() => true;

        #region Búsqueda y Filtros

        private void OnBusquedaTextChanged(object sender, TextChangedEventArgs e)
        {
            _viewModel.TextoBusqueda = e.NewTextValue ?? string.Empty;
        }

        private async void OnFiltroTapped(object sender, EventArgs e)
        {
            if (sender is Frame frame && frame.GestureRecognizers[0] is TapGestureRecognizer tap
                && tap.CommandParameter is string filtro)
            {
                _viewModel.FiltroEstado = filtro;
                ActualizarEstilosFiltros(filtro);
                await Task.CompletedTask;
            }
        }

        private void ActualizarEstilosFiltros(string filtroActivo)
        {
            var colorActivo = Color.FromArgb("#487CE4");
            var colorInactivo = App.Current.RequestedTheme == AppTheme.Dark
                ? Color.FromArgb("#3a3a3a")
                : Color.FromArgb("#FFFFFF");
            var bordeInactivo = App.Current.RequestedTheme == AppTheme.Dark
                ? Color.FromArgb("#38383A")
                : Color.FromArgb("#C6C6C8");
            var textoActivo = Colors.White;
            var textoInactivo = App.Current.RequestedTheme == AppTheme.Dark
                ? Color.FromArgb("#8E8E93")
                : Color.FromArgb("#8E8E93");

            void AplicarEstilo(Frame filtro, bool activo)
            {
                filtro.BackgroundColor = activo ? colorActivo : colorInactivo;
                filtro.BorderColor = activo ? Colors.Transparent : bordeInactivo;
                if (filtro.Content is Label lbl)
                {
                    lbl.TextColor = activo ? textoActivo : textoInactivo;
                }
            }

            AplicarEstilo(FiltroTodos, filtroActivo == "TODOS");
            AplicarEstilo(FiltroPendientes, filtroActivo == "PENDIENTES");
            AplicarEstilo(FiltroEntregados, filtroActivo == "ENTREGADOS");
        }

        #endregion

        #region Acciones de Tarjeta

        private async void OnEntregarTapped(object sender, EventArgs e)
        {
            if (_isLoading) return;

            try
            {
                if (sender is Label label && label.BindingContext is Encargo encargo)
                {
                    if (encargo.Estado == "ENTREGADO")
                    {
                        await DisplayAlert(Loc.Get("YaEntregado"), Loc.Get("EncargoYaEntregado"), "OK");
                        return;
                    }

                    bool confirmar = await DisplayAlert(
                        Loc.Get("MarcarEntregado"),
                        Loc.Format("ConfirmarMarcarEntregado", encargo.Nombre),
                        Loc.Get("SiEntregar"),
                        Loc.Get("Cancelar"));

                    if (confirmar)
                    {
                        _isLoading = true;
                        await _viewModel.MarcarEntregadoCommand.ExecuteAsync(encargo);
                        EncargosCollectionView.ItemsSource = _viewModel.Encargos;
                        await DisplayAlert(Loc.Get("Entregado"), Loc.Get("EncargoMarcadoEntregado"), "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await MostrarError(Loc.Get("ErrorAlEntregar"), ex.Message);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async void OnEditarTapped(object sender, EventArgs e)
        {
            if (_isLoading) return;

            try
            {
                if (sender is Label label && label.BindingContext is Encargo encargo)
                    await EditarEncargo(encargo);
            }
            catch (Exception ex)
            {
                await MostrarError(Loc.Get("ErrorEdicion"), ex.Message);
            }
        }

        private async void OnEliminarTapped(object sender, EventArgs e)
        {
            if (_isLoading) return;

            try
            {
                if (sender is Label label && label.BindingContext is Encargo encargo)
                    await EliminarEncargo(encargo);
            }
            catch (Exception ex)
            {
                await MostrarError(Loc.Get("ErrorEliminacion"), ex.Message);
            }
        }

        #endregion

        #region Contacto

        private async void OnContactTapped(object sender, EventArgs e)
        {
            if (_isLoading) return;

            try
            {
                if (sender is BindableObject element && element.BindingContext is Encargo encargo)
                {
                    if (string.IsNullOrWhiteSpace(encargo.Contacto))
                    {
                        await DisplayAlert(Loc.Get("SinNumero"),
                            Loc.Get("SinNumeroContacto"), "OK");
                        return;
                    }

                    string nombreCliente = encargo.Nombre ?? Loc.Get("ClienteDesconocido");
                    await MostrarOpcionesContacto(encargo.Contacto, nombreCliente);
                }
            }
            catch (Exception ex)
            {
                await MostrarError(Loc.Get("ErrorContacto"),
                    Loc.Format("NoSePudoAbrirAppContacto", ex.Message));
            }
        }

        private async Task MostrarOpcionesContacto(string telefono, string nombreCliente)
        {
            try
            {
                string telefonoLimpio = LimpiarNumeroTelefono(telefono);

                string accion = await DisplayActionSheet(
                    Loc.Format("ContactarCliente", nombreCliente),
                    Loc.Get("Cancelar"),
                    null,
                    Loc.Get("Llamar"),
                    Loc.Get("EnviarSMS"),
                    Loc.Get("CopiarNumero")
                );

                if (accion == Loc.Get("Llamar"))
                    await RealizarLlamada(telefonoLimpio);
                else if (accion == Loc.Get("EnviarSMS"))
                    await EnviarSMS(telefonoLimpio);
                else if (accion == Loc.Get("CopiarNumero"))
                    await CopiarNumero(telefonoLimpio);
            }
            catch (Exception ex)
            {
                await MostrarError(Loc.Get("ErrorContacto"), ex.Message);
            }
        }

        private async Task RealizarLlamada(string telefono)
        {
            try
            {
                PhoneDialer.Open(telefono);
            }
            catch (ArgumentNullException)
            {
                await DisplayAlert(Loc.Get("Error"), Loc.Get("NumeroTelefonoInvalido"), "OK");
            }
            catch (Exception ex)
            {
                await MostrarError(Loc.Get("ErrorAlLlamar"),
                    Loc.Format("NoSePudoRealizarLlamada", ex.Message));
            }
        }

        private async Task EnviarSMS(string telefono)
        {
            try
            {
                var message = new SmsMessage(Loc.Get("MensajeSmsEncargo"), telefono);
                await Sms.ComposeAsync(message);
            }
            catch (FeatureNotSupportedException)
            {
                await DisplayAlert(Loc.Get("NoCompatible"),
                    Loc.Get("SmsNoDisponible"), "OK");
            }
            catch (Exception ex)
            {
                await MostrarError(Loc.Get("ErrorAlEnviarSms"), ex.Message);
            }
        }

        private async Task CopiarNumero(string telefono)
        {
            try
            {
                await Clipboard.SetTextAsync(telefono);
                await DisplayAlert(Loc.Get("Copiado"),
                    Loc.Format("NumeroCopiado", telefono), "OK");
            }
            catch (Exception ex)
            {
                await MostrarError(Loc.Get("ErrorAlCopiar"), ex.Message);
            }
        }

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

        #endregion

        #region Navegación y Negocio

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
                }
            }
            catch (Exception ex)
            {
                await MostrarError(Loc.Get("ErrorNavegacion"),
                    Loc.Format("NoSePudoAbrirDetallesEncargo", ex.Message));
            }
        }

        private async Task EditarEncargo(Encargo encargo)
        {
            try
            {
                var editViewModel = _services.GetRequiredService<EditarEncargoViewModel>();
                await Navigation.PushAsync(new EditarEncargoPage(encargo, _repository, _imageStorageService, editViewModel));
            }
            catch (Exception ex)
            {
                await MostrarError(Loc.Get("ErrorAlEditar"),
                    Loc.Format("NoSePudoAbrirPaginaEdicion", ex.Message));
            }
        }

        private async Task EliminarEncargo(Encargo encargo)
        {
            try
            {
                string mensaje = Loc.Format("ConfirmarEliminarEncargo",
                                   encargo.Nombre,
                                   encargo.Contacto,
                                   Loc.FormatCurrency(encargo.Precio),
                                   encargo.Cantidad,
                                   Loc.FormatDate(encargo.Fecha),
                                   Loc.FormatDate(encargo.FechaEntrega)) +
                               $"\n\n{Loc.Get("AccionNoSePuedeDeshacer")}";

                bool confirmar = await DisplayAlert(Loc.Get("EliminarEncargo"), mensaje,
                    Loc.Get("SiEliminar"), Loc.Get("Cancelar"));

                if (confirmar)
                {
                    _isLoading = true;
                    await _viewModel.EliminarEncargoCommand.ExecuteAsync(encargo);
                    await DisplayAlert(Loc.Get("Exito"), Loc.Get("EncargoEliminado"), "OK");
                    await CargarEncargos();
                }
            }
            catch (Exception ex)
            {
                await MostrarError(Loc.Get("ErrorAlEliminar"),
                    Loc.Format("NoSePudoEliminarEncargo", ex.Message));
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async Task MostrarDetalles(Encargo encargo)
        {
            try
            {
                // Detalle eliminado - los datos se muestran en las tarjetas
            }
            catch (Exception ex)
            {
                await MostrarError(Loc.Get("ErrorAlMostrarDetalles"),
                    Loc.Format("NoSePudoAbrirDetallesPagina", ex.Message));
            }
        }

        private async Task ConcretarVenta(Encargo encargo)
        {
            try
            {
                string mensaje = Loc.Format("ConfirmarConcretarVenta",
                                   encargo.Nombre,
                                   encargo.Descripcion,
                                   Loc.FormatCurrency(encargo.Precio),
                                   encargo.Cantidad);

                bool confirmar = await DisplayAlert(Loc.Get("ConcretarVenta"), mensaje,
                    Loc.Get("SiConcretar"), Loc.Get("Cancelar"));

                if (confirmar)
                {
                    _isLoading = true;
                    await _viewModel.ConvertirEnVentaCommand.ExecuteAsync(encargo);
                    await DisplayAlert(Loc.Get("VentaConcretada"),
                        Loc.Get("VentaRegistrada"), "OK");
                    await CargarEncargos();
                }
            }
            catch (Exception ex)
            {
                await MostrarError(Loc.Get("ErrorAlConcretarVenta"),
                    Loc.Format("NoSePudoConcretarVenta", ex.Message));
            }
            finally
            {
                _isLoading = false;
            }
        }

        #endregion

        #region Utilidad

        private async Task MostrarError(string titulo, string mensaje)
        {
            await DisplayAlert(titulo, mensaje, "OK");
            Console.WriteLine($"{titulo}: {mensaje}");
        }

        public async Task RefrescarEncargos() => await CargarEncargos();

        #endregion
    }
}
