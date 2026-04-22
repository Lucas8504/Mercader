using Microcharts.Maui;
using Mercader.ViewModels;
using Mercader.Data.Interfaces;
using Mercader.Models;
using Mercader.Domain.Entities;
using Microsoft.Maui.Platform;
using System.Diagnostics;

namespace Mercader
{
    public partial class MainPage : ContentPage
    {
        private readonly MainViewModel _viewModel;
        private readonly IDataRepository _repository;

        public MainPage(IDataRepository repository, MainViewModel vm)
        {
            InitializeComponent();
            _repository = repository;
            _viewModel = vm;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            // Usar el Command del ViewModel
            if (_viewModel.CargarDatosCommand.CanExecute(null))
            {
                await _viewModel.CargarDatosCommand.ExecuteAsync(null);
            }

            // Scroll a los gráficos después de cargar
            await Task.WhenAll(
                ScrollToEndAsync(VentasScroll),
                ScrollToEndAsync(GastosScroll),
                ScrollToEndAsync(EncargosScroll),
                ScrollToEndAsync(GananciasScroll)
            );
        }

        #region Eventos de Navegación

        private async void InAgregarEncargo(object sender, EventArgs e)
        {
            try
            {
                var modal = new EncModal(_repository);
                await Navigation.PushModalAsync(modal);
                await _viewModel.CargarDatosCommand.ExecuteAsync(null);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo abrir el formulario: {ex.Message}", "OK");
            }
        }

        private async void InAgregarVenta(object sender, EventArgs e)
        {
            try
            {
                var modal = new VentaModal(_repository);
                await Navigation.PushModalAsync(modal);
                await _viewModel.CargarDatosCommand.ExecuteAsync(null);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo agregar la venta: {ex.Message}", "OK");
            }
        }

        private async void InAgregarGasto(object sender, EventArgs e)
        {
            try
            {
                var modal = new GastoModal(_repository);
                await Navigation.PushModalAsync(modal);
                await _viewModel.CargarDatosCommand.ExecuteAsync(null);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo abrir el modal: {ex.Message}", "OK");
            }
        }

        #endregion

        #region Gestión de Gráficos
        // Los gráficos ahora se actualizan automáticamente en el ViewModel vía RecalcularCommand

        private async Task ScrollToEndAsync(ScrollView scrollView)
        {
            if (scrollView?.Content != null)
            {
                await scrollView.ScrollToAsync(scrollView.Content, ScrollToPosition.End, animated: false);
            }
        }

        #endregion

        #region Exportación a Excel

        private async void OnExportarAExcelClicked(object sender, EventArgs e)
        {
            var botonExportar = sender as Button;
            if (botonExportar != null)
            {
                botonExportar.IsEnabled = false;
                botonExportar.Text = "⏳ Exportando...";
            }

            try
            {
                await _viewModel.CargarDatosCommand.ExecuteAsync(null);

                string carpetaPersonalizada = Path.Combine(FileSystem.Current.AppDataDirectory, "Exportaciones");
                Directory.CreateDirectory(carpetaPersonalizada);

                string nombreArchivo = $"Balance_Financiero_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                string rutaArchivo = Path.Combine(carpetaPersonalizada, nombreArchivo);

                var exportData = _viewModel.CrearExportDto();
                await ExportExcel.ExportarBalanceAExcelAsync(exportData, rutaArchivo);

                var mensaje = $"📊 ¡Reporte generado exitosamente!\n\n" +
                             $"📁 Archivo: {nombreArchivo}\n" +
                             $"📍 Ubicación: {carpetaPersonalizada}";

                var respuesta = await DisplayAlert("✅ Exportación Completada", mensaje, "📂 Abrir archivo", "✋ Cerrar");

                if (respuesta)
                {
                    await AbrirUbicacionArchivoAsync(rutaArchivo, carpetaPersonalizada);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("❌ Error en la exportación", $"No se pudo generar el reporte Excel:\n\n{ex.Message}", "Entendido");
                Debug.WriteLine($"Error detallado en exportación: {ex}");
            }
            finally
            {
                if (botonExportar != null)
                {
                    botonExportar.IsEnabled = true;
                    botonExportar.Text = "📊 EXPORTAR REPORTE COMPLETO";
                }
            }
        }

        private async Task AbrirUbicacionArchivoAsync(string rutaArchivo, string rutaCarpeta)
        {
            try
            {
                if (string.IsNullOrEmpty(rutaArchivo) || !File.Exists(rutaArchivo))
                {
                    await DisplayAlert("Error", "No se pudo encontrar el archivo.", "OK");
                    return;
                }

                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
                    await Launcher.OpenAsync(new OpenFileRequest { File = new ReadOnlyFile(rutaArchivo) });
                }
                else if (DeviceInfo.Platform == DevicePlatform.iOS)
                {
                    await DisplayAlert("Información", $"El archivo ha sido guardado en:\n{rutaCarpeta}", "OK");
                }
                else
                {
                    await Launcher.OpenAsync(new OpenFileRequest { File = new ReadOnlyFile(rutaCarpeta) });
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo abrir la ubicación del archivo: {ex.Message}", "OK");
            }
        }

        #endregion
    }
}
