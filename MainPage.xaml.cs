using Mercader.ViewModels;
using Mercader.Models.Domain;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform;

namespace Mercader
{
    public partial class MainPage : ContentPage
    {
        public MainViewModel VM { get; }

        public MainPage(MainViewModel vm)
        {
            InitializeComponent();
            VM = vm;
            BindingContext = VM;

            // Suscribirse a notificaciones de datos actualizados
            MessagingCenter.Subscribe<object>(this, "DataChanged", async _ =>
            {
                await VM.LoadDataAsync();
                await ScrollToEndAsync();
            });
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            MessagingCenter.Unsubscribe<object>(this, "DataChanged");
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await VM.LoadDataAsync();

            // Scroll al final para mostrar datos más recientes
            await Task.Delay(100); // Esperar que se rendericen los gráficos
            await ScrollToEndAsync();
        }

        private async Task ScrollToEndAsync()
        {
            try
            {
                VentasScroll?.ScrollToAsync(VentasScroll.Content, ScrollToPosition.End, false);
                GastosScroll?.ScrollToAsync(GastosScroll.Content, ScrollToPosition.End, false);
                EncargosScroll?.ScrollToAsync(EncargosScroll.Content, ScrollToPosition.End, false);
                GananciasScroll?.ScrollToAsync(GananciasScroll.Content, ScrollToPosition.End, false);
            }
            catch { /* Ignorar errores de scroll si las vistas no están listas */ }
        }

        // ===== Eventos de Navegación =====

        private async void InAgregarEncargo(object sender, EventArgs e)
        {
            try
            {
                var modal = new EncModal(VM);
                Navigation.PushModalAsync(modal);
                await Task.Delay(300);
                await ScrollToEndAsync();
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
                var modal = new VentaModal(VM);
                Navigation.PushModalAsync(modal);
                await Task.Delay(300);
                await ScrollToEndAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo abrir el formulario: {ex.Message}", "OK");
            }
        }

        private async void InAgregarGasto(object sender, EventArgs e)
        {
            try
            {
                var modal = new GastoModal(VM);
                Navigation.PushModalAsync(modal);
                await Task.Delay(300);
                await ScrollToEndAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo abrir el formulario: {ex.Message}", "OK");
            }
        }

        // ===== Exportación =====

        private async void OnExportarAExcelClicked(object sender, EventArgs e)
        {
            try
            {
                var botonExportar = sender as Button;
                botonExportar!.IsEnabled = false;
                botonExportar.Text = "⏳ Exportando...";

                string carpeta = Path.Combine(FileSystem.Current.AppDataDirectory, "Exportaciones");
                Directory.CreateDirectory(carpeta);

                string nombreArchivo = $"Balance_Financiero_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                string rutaArchivo = Path.Combine(carpeta, nombreArchivo);

                // Refrescar datos antes de exportar
                await VM.LoadDataAsync();
                var exportData = VM.CrearExportDto();
                await ExportExcel.ExportarBalanceAExcelAsync(exportData, rutaArchivo);

                var respuesta = await DisplayAlert(
                    "✅ Exportación Completada",
                    $"📁 Archivo: {nombreArchivo}\n📍 Ubicación: {carpeta}",
                    "📂 Abrir archivo",
                    "✋ Cerrar"
                );

                if (respuesta)
                {
                    await Launcher.OpenAsync(new OpenFileRequest
                    {
                        File = new ReadOnlyFile(rutaArchivo)
                    });
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("❌ Error", $"No se pudo exportar: {ex.Message}", "OK");
            }
            finally
            {
                if (sender is Button boton)
                {
                    boton.IsEnabled = true;
                    boton.Text = "📊 EXPORTAR REPORTE COMPLETO";
                }
            }
        }
    }
}
