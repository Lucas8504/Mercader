using Microcharts.Maui;
using SkiaSharp;
using Mercader.Helpers;
using System.Globalization;
using Microcharts;

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

                // Añadir esta línea para actualizar gráficos
                await ActualizarGraficosAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar datos: {ex.Message}");
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

        private async void PeriodSelector_OnSelectedIndexChanged(object sender, EventArgs e)
        {
            await ActualizarGraficosAsync();
        }

        private Task ActualizarGraficosAsync()
        {
            try
            {
                // Obtener período seleccionado
                var periodo = PeriodSelector.SelectedItem?.ToString() ?? "Meses";

                // Obtener datos actuales de balance (ya cargados en labels)
                var ventas = balance.Ventas;
                var gastos = balance.Gastos;

                // Agrupar según período
                var ventasAgrupadas = periodo switch
                {
                    "Días" => AgruparPorDia(ventas),
                    "Semanas" => AgruparPorSemana(ventas),
                    _ => AgruparPorMes(ventas)
                };

                var gastosAgrupados = periodo switch
                {
                    "Días" => AgruparPorDia(gastos),
                    "Semanas" => AgruparPorSemana(gastos),
                    _ => AgruparPorMes(gastos)
                };



                // Configurar gráficos con los datos agrupados
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ConfigurarGraficoVentas(ventasAgrupadas, periodo);
                    ConfigurarGraficoGastos(gastosAgrupados, periodo);
                    ConfigurarGraficoGanancias(ventasAgrupadas, gastosAgrupados, periodo);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex}");
            }

            return Task.CompletedTask;
        }


        // Métodos de agrupación
        private List<(string Periodo, decimal Total)> AgruparPorDia(List<Ventas> ventas)
        {
            return ventas
                .GroupBy(v => v.Fecha.ToString("yyyy-MM-dd"))
                .Select(g => (g.Key, Total: g.Sum(v => v.Precio * v.Cantidad)))
                .ToList();
        }

        private List<(string Periodo, decimal Total)> AgruparPorSemana(List<Ventas> ventas)
        {
            return ventas
                .GroupBy(v => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                    v.Fecha, CalendarWeekRule.FirstDay, DayOfWeek.Monday))
                .Select(g => ($"Semana {g.Key}", Total: g.Sum(v => v.Precio * v.Cantidad)))
                .ToList();
        }

        private List<(string Periodo, decimal Total)> AgruparPorMes(List<Ventas> ventas)
        {
            return ventas
                .GroupBy(v => v.Fecha.ToString("yyyy-MM"))
                .Select(g => (g.Key, Total: g.Sum(v => v.Precio * v.Cantidad)))
                .ToList();
        }

        private List<(string Periodo, decimal Total)> AgruparPorDia(List<Gasto> gastos)
        {
            return gastos
                .GroupBy(g => g.Fecha.ToString("yyyy-MM-dd"))
                .Select(g => (g.Key, Total: g.Sum(x => x.Monto * x.Cantidad)))
                .ToList();
        }

        private List<(string Periodo, decimal Total)> AgruparPorSemana(List<Gasto> gastos)
        {
            return gastos
                .GroupBy(g => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                    g.Fecha, CalendarWeekRule.FirstDay, DayOfWeek.Monday))
                .Select(g => ($"Semana {g.Key}", Total: g.Sum(x => x.Monto * x.Cantidad)))
                .ToList();
        }

        private List<(string Periodo, decimal Total)> AgruparPorMes(List<Gasto> gastos)
        {
            return gastos
                .GroupBy(g => g.Fecha.ToString("yyyy-MM"))
                .Select(g => (g.Key, Total: g.Sum(x => x.Monto * x.Cantidad)))
                .ToList();
        }

        private string FormatearEtiqueta(string periodo, string tipo)
        {
            return tipo switch
            {
                "Días" => DateTime.ParseExact(periodo, "yyyy-MM-dd", CultureInfo.InvariantCulture)
                    .ToString("dd MMM"),
                "Semanas" => periodo.Replace("Semana ", "Sem "),
                _ => DateTime.ParseExact(periodo, "yyyy-MM", CultureInfo.InvariantCulture)
                    .ToString("MMM")
            };
        }

        private void ConfigurarGraficoVentas(List<(string Periodo, decimal Total)> datos, string periodo)
        {
            // Crear datos dummy si no hay registros
            if (!datos.Any() || datos.All(d => d.Total == 0))
            {
                datos = periodo switch
                {
                    "Días" => Enumerable.Range(0, 7)
                        .Select(i => DateTime.Now.AddDays(-i).ToString("yyyy-MM-dd"))
                        .Reverse()
                        .Select(d => (d, 0m))
                        .ToList(),
                    "Semanas" => Enumerable.Range(0, 6)
                        .Select(i => $"Semana {DateTime.Now.AddDays(-7 * i):ww}")
                        .Reverse()
                        .Select(d => (d, 0m))
                        .ToList(),
                    _ => BalanceHelper.ObtenerUltimos6Meses()
                        .Select(m => (m, 0m))
                        .ToList()
                };
            }

            var entries = datos.Select(d => new ChartEntry((float)d.Total)
            {
                Label = FormatearEtiqueta(d.Periodo, periodo),
                ValueLabel = d.Total.ToString("C0"),
                Color = SKColor.Parse("#00E82A"),
                TextColor = SKColors.White
            }).ToArray();

            VentasChart.Chart = new LineChart
            {
                Entries = entries,
                LabelTextSize = 20,
                BackgroundColor = SKColor.Parse("#2a2a2a"),
                LineSize = 2,
                IsAnimated = true
            };
        }



        private void ConfigurarGraficoGastos(List<(string Mes, decimal Total)> datos, string periodo)
        {
            // Crear datos dummy si no hay registros
            if (!datos.Any() || datos.All(d => d.Total == 0))
            {
                datos = periodo switch
                {
                    "Días" => Enumerable.Range(0, 7)
                        .Select(i => DateTime.Now.AddDays(-i).ToString("yyyy-MM-dd"))
                        .Reverse()
                        .Select(d => (d, 0m))
                        .ToList(),
                    "Semanas" => Enumerable.Range(0, 6)
                        .Select(i => $"Semana {DateTime.Now.AddDays(-7 * i):ww}")
                        .Reverse()
                        .Select(d => (d, 0m))
                        .ToList(),
                    _ => BalanceHelper.ObtenerUltimos6Meses()
                        .Select(m => (m, 0m))
                        .ToList()
                };
            }
            var entries = datos.Select(d => new ChartEntry((float)d.Total)
            {
                Label = FormatearEtiqueta(d.Mes, periodo),
                ValueLabel = d.Total.ToString("C0"),
                Color = SKColor.Parse("#FF0000"),
                TextColor = SKColors.White
            }).ToArray();
            GastosChart.Chart = new LineChart
            {
                Entries = entries,
                LabelTextSize = 20,
                BackgroundColor = SKColor.Parse("#2a2a2a"),
                LineSize = 2,
                IsAnimated = true
            };
        }

        private void ConfigurarGraficoGanancias(List<(string Mes, decimal Total)> ventas, List<(string Mes, decimal Total)> gastos, string periodo)
        {
            var ganancias = ventas.Zip(gastos, (v, g) => (v.Mes, Ganancia: v.Total - g.Total)).ToList();
            // Crear datos dummy si no hay registros
            if (!ganancias.Any() || ganancias.All(d => d.Ganancia == 0))
            {
                ganancias = periodo switch
                {
                    "Días" => Enumerable.Range(0, 7)
                        .Select(i => DateTime.Now.AddDays(-i).ToString("yyyy-MM-dd"))
                        .Reverse()
                        .Select(d => (d, 0m))
                        .ToList(),
                    "Semanas" => Enumerable.Range(0, 6)
                        .Select(i => $"Semana {DateTime.Now.AddDays(-7 * i):ww}")
                        .Reverse()
                        .Select(d => (d, 0m))
                        .ToList(),
                    _ => BalanceHelper.ObtenerUltimos6Meses()
                        .Select(m => (m, 0m))
                        .ToList()
                };
            }
            var entries = ganancias.Select(d => new ChartEntry((float)d.Ganancia)
            {
                Label = FormatearEtiqueta(d.Mes, periodo),
                ValueLabel = d.Ganancia.ToString("C0"),
                Color = SKColor.Parse("#00E82A"),
                TextColor = SKColors.White
            }).ToArray();
            GananciasChart.Chart = new LineChart
            {
                Entries = entries,
                LabelTextSize = 20,
                BackgroundColor = SKColor.Parse("#2a2a2a"),
                LineSize = 2,
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