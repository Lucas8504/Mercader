using Microcharts.Maui;
using Mercader.ViewModels;
using Mercader.Models;
using Mercader.Models.Domain;
using Microsoft.Maui.Platform;
using System.Diagnostics;

namespace Mercader
{
    public partial class MainPage : ContentPage
    {
        private readonly MainViewModel _viewModel;
        private readonly DataRepository _repo;

        public MainPage(DataRepository repo, MainViewModel vm)
        {
            InitializeComponent();
            _repo = repo;
            _viewModel = vm;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarDatosAsync();
        }

        private async Task CargarDatosAsync()
        {
            try
            {
                var encargos = await _repo.GetEncargosAsync();
                var gastos = await _repo.GetGastosAsync();
                var ventas = await _repo.GetVentasAsync();

                // Cargar datos al ViewModel (MVVM puro)
                _viewModel.Encargos.Clear();
                foreach (var e in encargos) _viewModel.Encargos.Add(e);

                _viewModel.Gastos.Clear();
                foreach (var g in gastos) _viewModel.Gastos.Add(g);

                _viewModel.Ventas.Clear();
                foreach (var v in ventas) _viewModel.Ventas.Add(v);

                // El ViewModel usa BalanceCalculatorService internamente
                _viewModel.Recalcular();

                // Actualizar gráficos usando los datos ya calculados por el VM
                await ActualizarGraficosAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar datos: {ex.Message}");
                await DisplayAlert("Error", $"No se pudieron cargar los datos: {ex.Message}", "OK");
            }
        }

        #region Eventos de Navegación

        private async void InAgregarEncargo(object sender, EventArgs e)
        {
            try
            {
                var modal = new EncModal(_repo);
                await Navigation.PushModalAsync(modal);
                await CargarDatosAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo abrir el formulario de encargo: {ex.Message}", "OK");
            }
        }

        private async void InAgregarVenta(object sender, EventArgs e)
        {
            try
            {
                var modal = new VentaModal(_repo);
                await Navigation.PushModalAsync(modal);
                await CargarDatosAsync();
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
                var modal = new GastoModal(_repo);
                await Navigation.PushModalAsync(modal);
                await CargarDatosAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo abrir el modal: {ex.Message}", "OK");
            }
        }

        #endregion

        #region Gestión de Gráficos

        private async Task ActualizarGraficosAsync()
        {
            try
            {
                // Los datos ya están agrupados por el BalanceCalculatorService en VM.Recalcular()
                await _viewModel.ActualizarGraficosAsync();

                // Scroll a los gráficos
                await Task.WhenAll(
                    ScrollToEndAsync(VentasScroll),
                    ScrollToEndAsync(GastosScroll),
                    ScrollToEndAsync(EncargosScroll),
                    ScrollToEndAsync(GananciasScroll)
                );
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al actualizar gráficos: {ex}");
            }
        }

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
                await CargarDatosAsync();

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
