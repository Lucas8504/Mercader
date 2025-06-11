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

                await ActualizarGraficosAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar datos: {ex.Message}");
            }
        }

        // ... métodos de actualización de etiquetas existentes ...
        public void ActualizarEtiquetaGanancias()
        {
            var ganancias = balance.CalcularGanancias();
            GananciasLabel.Text = $"{ganancias:C}";
        }

        public void ActualizarEtiquetaVentas()
        {
            decimal ventas = balance.CalcularVentas();
            VentasLabel.Text = $" {ventas:C}";
        }

        public void ActualizarEtiquetaGastos()
        {
            decimal gastos = balance.CalcularGastos();
            GastosLabel.Text = $" {gastos:C}";
        }

        public void ActualizarEtiquetaEncargos()
        {
            decimal encargo = balance.CalcularEncargos();
            EncargosLabel.Text = $" {encargo:C}";
        }

        // ... métodos de navegación existentes ...
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

        private async void PeriodSelector_OnSelectedIndexChanged(object sender, EventArgs e)
        {
            await ActualizarGraficosAsync();
        }

        private Task ActualizarGraficosAsync()
        {
            try
            {
                var periodo = PeriodSelector.SelectedItem?.ToString() ?? "Meses";
                var ventas = balance.Ventas;
                var gastos = balance.Gastos;

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

        // MÉTODOS DE AGRUPACIÓN MEJORADOS
        private List<(string Periodo, decimal Total)> AgruparPorDia(List<Ventas> ventas)
        {
            var hoy = DateTime.Today;
            var ultimosDias = Enumerable.Range(0, 7)
                .Select(i => hoy.AddDays(-i))
                .Reverse()
                .ToList();

            return ultimosDias.Select(fecha =>
            {
                var ventasDia = ventas.Where(v => v.Fecha.Date == fecha.Date);
                var total = ventasDia.Sum(v => v.Precio * v.Cantidad);
                return (fecha.ToString("dd/MM"), total);
            }).ToList();
        }

        private List<(string Periodo, decimal Total)> AgruparPorSemana(List<Ventas> ventas)
        {
            var hoy = DateTime.Today;
            var ultimasSemanas = Enumerable.Range(0, 6)
                .Select(i =>
                {
                    var inicioSemana = hoy.AddDays(-7 * i).AddDays(-(int)hoy.AddDays(-7 * i).DayOfWeek);
                    var finSemana = inicioSemana.AddDays(6);
                    return new { Inicio = inicioSemana, Fin = finSemana };
                })
                .Reverse()
                .ToList();

            return ultimasSemanas.Select(semana =>
            {
                var ventasSemana = ventas.Where(v => v.Fecha.Date >= semana.Inicio && v.Fecha.Date <= semana.Fin);
                var total = ventasSemana.Sum(v => v.Precio * v.Cantidad);
                return ($"{semana.Inicio:dd/MM}", total);
            }).ToList();
        }

        private List<(string Periodo, decimal Total)> AgruparPorMes(List<Ventas> ventas)
        {
            var hoy = DateTime.Today;
            var ultimosMeses = Enumerable.Range(0, 6)
                .Select(i => hoy.AddMonths(-i))
                .Reverse()
                .ToList();

            return ultimosMeses.Select(mes =>
            {
                var ventasMes = ventas.Where(v => v.Fecha.Year == mes.Year && v.Fecha.Month == mes.Month);
                var total = ventasMes.Sum(v => v.Precio * v.Cantidad);
                return (mes.ToString("MMM"), total);
            }).ToList();
        }

        private List<(string Periodo, decimal Total)> AgruparPorDia(List<Gasto> gastos)
        {
            var hoy = DateTime.Today;
            var ultimosDias = Enumerable.Range(0, 7)
                .Select(i => hoy.AddDays(-i))
                .Reverse()
                .ToList();

            return ultimosDias.Select(fecha =>
            {
                var gastosDia = gastos.Where(g => g.Fecha.Date == fecha.Date);
                var total = gastosDia.Sum(g => g.Monto * g.Cantidad);
                return (fecha.ToString("dd/MM"), total);
            }).ToList();
        }

        private List<(string Periodo, decimal Total)> AgruparPorSemana(List<Gasto> gastos)
        {
            var hoy = DateTime.Today;
            var ultimasSemanas = Enumerable.Range(0, 6)
                .Select(i =>
                {
                    var inicioSemana = hoy.AddDays(-7 * i).AddDays(-(int)hoy.AddDays(-7 * i).DayOfWeek);
                    var finSemana = inicioSemana.AddDays(6);
                    return new { Inicio = inicioSemana, Fin = finSemana };
                })
                .Reverse()
                .ToList();

            return ultimasSemanas.Select(semana =>
            {
                var gastosSemana = gastos.Where(g => g.Fecha.Date >= semana.Inicio && g.Fecha.Date <= semana.Fin);
                var total = gastosSemana.Sum(g => g.Monto * g.Cantidad);
                return ($"{semana.Inicio:dd/MM}", total);
            }).ToList();
        }

        private List<(string Periodo, decimal Total)> AgruparPorMes(List<Gasto> gastos)
        {
            var hoy = DateTime.Today;
            var ultimosMeses = Enumerable.Range(0, 6)
                .Select(i => hoy.AddMonths(-i))
                .Reverse()
                .ToList();

            return ultimosMeses.Select(mes =>
            {
                var gastosMes = gastos.Where(g => g.Fecha.Year == mes.Year && g.Fecha.Month == mes.Month);
                var total = gastosMes.Sum(g => g.Monto * g.Cantidad);
                return (mes.ToString("MMM"), total);
            }).ToList();
        }

        // CONFIGURACIÓN DE GRÁFICOS MEJORADA
        private void ConfigurarGraficoVentas(List<(string Periodo, decimal Total)> datos, string periodo)
        {
            var entries = datos.Select(d => new ChartEntry((float)d.Total)
            {
                Label = d.Periodo,
                ValueLabel = FormatearValor(d.Total),
                Color = SKColor.Parse("#2e9449"), // Verde más suave
                TextColor = SKColor.Parse("#E0E0E0"), // Gris claro para mejor legibilidad
                ValueLabelColor = SKColor.Parse("#FFFFFF") // Blanco para valores
            }).ToArray();

            VentasChart.Chart = new LineChart
            {
                Entries = entries,
                LabelTextSize = 24, // Texto más grande
                ValueLabelTextSize = 20,
                BackgroundColor = SKColor.Parse("#2a2a2a"),
                LineSize = 4, // Línea más gruesa
                PointSize = 12, // Puntos más grandes
                IsAnimated = true,
                AnimationDuration = TimeSpan.FromMilliseconds(800),
                LabelOrientation = Orientation.Horizontal,
                ValueLabelOrientation = Orientation.Horizontal,
                // Añadir márgenes para mejor visualización
                Margin = 40,
                // Mostrar línea de referencia en cero
                ShowYAxisLines = true,
                ShowYAxisText = true
            };
        }

        private void ConfigurarGraficoGastos(List<(string Periodo, decimal Total)> datos, string periodo)
        {
            var entries = datos.Select(d => new ChartEntry((float)d.Total)
            {
                Label = d.Periodo,
                ValueLabel = FormatearValor(d.Total),
                Color = SKColor.Parse("#d63384"), // Rojo más suave
                TextColor = SKColor.Parse("#E0E0E0"),
                ValueLabelColor = SKColor.Parse("#FFFFFF")
            }).ToArray();

            GastosChart.Chart = new LineChart
            {
                Entries = entries,
                LabelTextSize = 24,
                ValueLabelTextSize = 20,
                BackgroundColor = SKColor.Parse("#2a2a2a"),
                LineSize = 4,
                PointSize = 12,
                IsAnimated = true,
                AnimationDuration = TimeSpan.FromMilliseconds(800),
                LabelOrientation = Orientation.Horizontal,
                ValueLabelOrientation = Orientation.Horizontal,
                Margin = 40,
                 // Mostrar línea de referencia en cero
                ShowYAxisLines = true,
                ShowYAxisText = true
            };
        }

        private void ConfigurarGraficoGanancias(List<(string Periodo, decimal Total)> ventas,
                                                List<(string Periodo, decimal Total)> gastos,
                                                string periodo)
        {
            // Combinar datos asegurando que coincidan los períodos
            var datosCompletos = ventas.Select(v =>
            {
                var gastoCorrespondiente = gastos.FirstOrDefault(g => g.Periodo == v.Periodo);
                var ganancia = v.Total - (gastoCorrespondiente.Total);
                return new { Periodo = v.Periodo, Ganancia = ganancia };
            }).ToList();

            var entries = datosCompletos.Select(d => new ChartEntry((float)d.Ganancia)
            {
                Label = d.Periodo,
                ValueLabel = FormatearValor(d.Ganancia),
                // Color dinámico: verde para ganancias positivas, rojo para negativas
                Color = d.Ganancia >= 0 ? SKColor.Parse("#1f6bc2") : SKColor.Parse("#dc3545"),
                TextColor = SKColor.Parse("#E0E0E0"),
                ValueLabelColor = SKColor.Parse("#FFFFFF")
            }).ToArray();

            GananciasChart.Chart = new LineChart
            {
                Entries = entries,
                LabelTextSize = 24,
                ValueLabelTextSize = 20,
                BackgroundColor = SKColor.Parse("#2a2a2a"),
                LineSize = 4,
                PointSize = 12,
                IsAnimated = true,
                AnimationDuration = TimeSpan.FromMilliseconds(800),
                LabelOrientation = Orientation.Horizontal,
                ValueLabelOrientation = Orientation.Horizontal,
                Margin = 40,
                // Mostrar línea de referencia en cero
                ShowYAxisLines = true,
                ShowYAxisText = true
            };
        }

        // MÉTODO AUXILIAR PARA FORMATEAR VALORES
        private string FormatearValor(decimal valor)
        {
            if (Math.Abs(valor) >= 1000000)
                return $"{valor / 1000000:F1}M";
            else if (Math.Abs(valor) >= 1000)
                return $"{valor / 1000:F1}K";
            else
                return valor.ToString("C0");
        }

        // ... resto de métodos existentes (exportar, etc.) ...
        private async void OnExportarAExcelClicked(object sender, EventArgs e)
        {
            try
            {
                string carpetaPersonalizada = Path.Combine(FileSystem.Current.AppDataDirectory, "Exportaciones");

                if (!Directory.Exists(carpetaPersonalizada))
                {
                    Directory.CreateDirectory(carpetaPersonalizada);
                }

                string nombreArchivo = $"balance_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                string rutaArchivo = Path.Combine(carpetaPersonalizada, nombreArchivo);

                await ExportExcel.ExportarBalanceAExcelAsync(balance, rutaArchivo);
                await DisplayAlert("Exportación Completa", $"Archivo exportado a {rutaArchivo}", "OK");
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
                    await Launcher.OpenAsync(new OpenFileRequest
                    {
                        File = new ReadOnlyFile(rutaArchivo)
                    });
                }
                else if (DeviceInfo.Platform == DevicePlatform.iOS)
                {
                    await DisplayAlert("Información",
                        $"El archivo ha sido guardado en:\n{rutaCarpeta}\n\nPuedes acceder a él mediante tu aplicación de archivos.",
                        "OK");
                }
                else
                {
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
    }
}