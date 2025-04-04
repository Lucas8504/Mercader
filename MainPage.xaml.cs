using Microcharts.Maui;
using SkiaSharp;
using Mercader.Helpers;

namespace Mercader
{
    public partial class MainPage : ContentPage
    {
        public Balance balance;
        private readonly DataRepository _dataRepo;


        public MainPage(DataRepository _dataRepo)
        {
            InitializeComponent();
            balance = new Balance();
            this._dataRepo = _dataRepo;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Cargar datos de la base de datos cada vez que la página aparece
            CargarDatosAsync().ConfigureAwait(false);
        }

        private async Task CargarDatosAsync()
        {
            try
            {
                var encargos = await App.DataRepo.GetEncargosAsync();
                var gastos = await App.DataRepo.GetGastosAsync();
                var ventas = await App.DataRepo.GetVentasAsync();

                balance.Encargos = encargos;
                balance.Gastos = gastos;
                balance.Ventas = ventas;

                ActualizarEtiquetaEncargos();
                ActualizarEtiquetaGastos();
                ActualizarEtiquetaVentas();
                ActualizarEtiquetaGanancias();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al cargar datos: {ex.Message}", "OK");
            }
        }

        // Método público para actualizar la etiqueta de ganancias
        public void ActualizarEtiquetaGanancias()
        {
           
                var ganancias = balance.CalcularGanancias();
                GananciasLabel.Text = $"{ganancias:C}";
           
        }

        // Método público para actualizar la etiqueta de ventas
        public void ActualizarEtiquetaVentas()
        {
           
                decimal ventas = balance.CalcularVentas();
                VentasLabel.Text = $" {ventas:C}";
           
        }

        // Método público para actualizar la etiqueta de gastos
        public void ActualizarEtiquetaGastos()
        {
           
            
                decimal gastos = balance.CalcularGastos();
                GastosLabel.Text = $" {gastos:C}";
           
        }

        // Método público para actualizar la etiqueta de encargos
        public void ActualizarEtiquetaEncargos()
        {
            
                decimal encargo = balance.CalcularEncargos();
                EncargosLabel.Text = $" {encargo:C}";
           
        }

        private async void InAgregarEncargo(object sender, EventArgs e)
        {
            var encargoModal = new EncModal(this);
            await Navigation.PushModalAsync(encargoModal);
            var nuevoEncargo = encargoModal.Encargo;
            if (nuevoEncargo != null)
            {
                await App.DataRepo.SaveEncargoAsync(nuevoEncargo);
                balance.Encargos.Add(nuevoEncargo);
                ActualizarEtiquetaEncargos();
            }
        }

        private async void InAgregarVenta(object sender, EventArgs e)
        {
            var ventaModal = new VentaModal(this);
            await Navigation.PushModalAsync(ventaModal);
            var nuevaVenta = ventaModal.Venta;
            if (nuevaVenta != null)
            {
                await App.DataRepo.SaveVentasAsync(nuevaVenta);
                balance.Ventas.Add(nuevaVenta);
                ActualizarEtiquetaVentas();
            }
        }

        private async void InAgregarGasto(object sender, EventArgs e)
        {
            var gastoModal = new GastoModal(this);
            await Navigation.PushModalAsync(gastoModal);
            var nuevoGasto = gastoModal.Gasto;
            if (nuevoGasto != null)
            {
                await App.DataRepo.SaveGastoAsync(nuevoGasto);
                balance.Gastos.Add(nuevoGasto);
                ActualizarEtiquetaGastos();
            }
        }

        private void OnCalcularGananciasClicked(object sender, EventArgs e)
        {
           
                var ganancias = balance.CalcularGanancias();
                GananciasLabel.Text = $"Ganancias: {ganancias:C}";
           
        }

        private async Task ActualizarGraficosAsync()
        {
            try
            {
                var ventas = await _dataRepo.GetVentasUltimos6MesesAsync();
                var gastos = await _dataRepo.GetGastosUltimos6MesesAsync();
                var mesesRequeridos = BalanceHelper.ObtenerUltimos6Meses();

                var ventasPorMes = BalanceHelper.RellenarMesesFaltantes(
                    BalanceHelper.AgruparVentasPorMes(ventas), mesesRequeridos);

                var gastosPorMes = BalanceHelper.RellenarMesesFaltantes(
                    BalanceHelper.AgruparGastosPorMes(gastos), mesesRequeridos);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ConfigurarGraficoVentas(ventasPorMes);
                    ConfigurarGraficoGastos(gastosPorMes);
                    ConfigurarGraficoGanancias(ventasPorMes, gastosPorMes);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error actualizando gráficos: {ex.Message}");
            }
        }

        private void ConfigurarGraficoVentas(List<(string Mes, decimal Total)> datos)
        {
            var entries = datos.Select(d => new ChartEntry((float)d.Total)
            {
                Label = DateTime.ParseExact(d.Mes, "yyyy-MM", CultureInfo.InvariantCulture).ToString("MMM"),
                ValueLabel = d.Total >= 1000 ? $"{d.Total / 1000:F1}k" : d.Total.ToString("F0"),
                Color = SKColor.Parse("#2e9449"),
                TextColor = SKColors.White,
                ValueLabelColor = SKColors.White
            }).ToArray();

            VentasChart.Chart = new LineChart
            {
                Entries = entries,
                LabelTextSize = 24,
                BackgroundColor = SKColor.Parse("#2a2a2a"),
                LabelColor = SKColors.White,
                LineSize = 6,
                PointMode = PointMode.Circle,
                PointSize = 16,
                IsAnimated = true
            };
        }

        private void ConfigurarGraficoGastos(List<(string Mes, decimal Total)> datos)
        {
            var entries = datos.Select(d => new ChartEntry((float)d.Total)
            {
                Label = DateTime.ParseExact(d.Mes, "yyyy-MM", CultureInfo.InvariantCulture).ToString("MMM"),
                ValueLabel = d.Total >= 1000 ? $"{d.Total / 1000:F1}k" : d.Total.ToString("F0"),
                Color = SKColor.Parse("#6e0a24"),
                TextColor = SKColors.White,
                ValueLabelColor = SKColors.White
            }).ToArray();

            GastosChart.Chart = new BarChart
            {
                Entries = entries,
                LabelTextSize = 24,
                BackgroundColor = SKColor.Parse("#2a2a2a"),
                LabelColor = SKColors.White,
                BarAreaAlpha = 120,
                IsAnimated = true
            };
        }

        private void ConfigurarGraficoGanancias(List<(string Mes, decimal TotalVentas)> ventas,
                                              List<(string Mes, decimal TotalGastos)> gastos)
        {
            var ganancias = ventas.Zip(gastos, (v, g) => (
                Mes: v.Mes,
                Ganancia: v.TotalVentas - g.TotalGastos
            )).ToList();

            var entries = ganancias.Select(g => new ChartEntry((float)g.Ganancia)
            {
                Label = DateTime.ParseExact(g.Mes, "yyyy-MM", CultureInfo.InvariantCulture).ToString("MMM"),
                ValueLabel = g.Ganancia >= 1000 ? $"{g.Ganancia / 1000:F1}k" :
                             g.Ganancia <= -1000 ? $"{g.Ganancia / 1000:F1}k" : g.Ganancia.ToString("F0"),
                Color = g.Ganancia >= 0 ? SKColor.Parse("#1f6bc2") : SKColor.Parse("#d32f2f"),
                TextColor = SKColors.White,
                ValueLabelColor = SKColors.White
            }).ToArray();

            GananciasChart.Chart = new LineChart
            {
                Entries = entries,
                LabelTextSize = 24,
                BackgroundColor = SKColor.Parse("#2a2a2a"),
                LabelColor = SKColors.White,
                LineMode = LineMode.Spline,
                PointMode = PointMode.Square,
                PointSize = 16,
                IsAnimated = true
            };
        }


        private async void OnExportarAExcelClicked(object sender, EventArgs e)
        {
            try
            {
                // Definir la carpeta donde se guardará el archivo
                string carpetaPersonalizada = Path.Combine(FileSystem.Current.AppDataDirectory, "Exportaciones");

                // Crear la carpeta si no existe
                if (!Directory.Exists(carpetaPersonalizada))
                {
                    Directory.CreateDirectory(carpetaPersonalizada);
                }

                // Definir la ruta completa del archivo
                string nombreArchivo = $"balance_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                string rutaArchivo = Path.Combine(carpetaPersonalizada, nombreArchivo);

                // Exportar el balance a Excel
                await ExportExcel.ExportarBalanceAExcelAsync(balance, rutaArchivo);

                // Mostrar mensaje de éxito
                await DisplayAlert("Exportación Completa", $"Archivo exportado a {rutaArchivo}", "OK");

                // Abrir la ubicación (carpeta) donde se guardó el archivo
                await AbrirUbicacionArchivoAsync(rutaArchivo, carpetaPersonalizada);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo exportar el archivo: {ex.Message}", "OK");
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
                    // Intentar abrir el archivo directamente
                    await Launcher.OpenAsync(new OpenFileRequest
                    {
                        File = new ReadOnlyFile(rutaArchivo)
                    });


                }
                else if (DeviceInfo.Platform == DevicePlatform.iOS)
                {
                    // En iOS, también mostramos un mensaje informativo
                    await DisplayAlert("Información",
                        $"El archivo ha sido guardado en:\n{rutaCarpeta}\n\nPuedes acceder a él mediante tu aplicación de archivos.",
                        "OK");
                }
                else
                {
                    // Para otras plataformas, intentamos abrir la carpeta directamente
                    await Launcher.OpenAsync(new OpenFileRequest
                    {
                        File = new ReadOnlyFile(rutaCarpeta)
                    });
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo abrir la ubicación del archivo: {ex.Message}", "OK");
            }
        }

        private async void OnVerEncargosClicked(object sender, EventArgs e)
        {
            var navigationParameter = new Dictionary<string, object>
                {
                    { "MainPage", this }
                };
            await Shell.Current.GoToAsync(nameof(Encargos), navigationParameter);
        }
    }
}
