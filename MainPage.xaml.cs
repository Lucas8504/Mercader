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

        public MainPage(DataRepository dataRepo)
        {
            InitializeComponent();
            balance = new Balance();
            this._dataRepo = dataRepo;

            // Configurar el selector de período con el valor por defecto
            PeriodSelector.SelectedIndex = 2; // "Meses" por defecto
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

                // Actualizar todas las etiquetas en el hilo principal
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ActualizarEtiquetaEncargos();
                    ActualizarEtiquetaGastos();
                    ActualizarEtiquetaVentas();
                    ActualizarEtiquetaGanancias();
                    ActualizarEtiquetasPeriodo();
                });

                await ActualizarGraficosAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar datos: {ex.Message}");
                await DisplayAlert("Error", $"No se pudieron cargar los datos: {ex.Message}", "OK");
            }
        }

        #region Actualización de Etiquetas

        public void ActualizarEtiquetaGanancias()
        {
            var ganancias = balance.CalcularGanancias();
            GananciasLabel.Text = $"{ganancias:C}";
        }

        public void ActualizarEtiquetaVentas()
        {
            decimal ventas = balance.CalcularVentas();
            VentasLabel.Text = $"{ventas:C}";
        }

        public void ActualizarEtiquetaGastos()
        {
            decimal gastos = balance.CalcularGastos();
            GastosLabel.Text = $"{gastos:C}";
        }

        public void ActualizarEtiquetaEncargos()
        {
            decimal encargo = balance.CalcularEncargos();
            EncargosLabel.Text = $"{encargo:C}";
        }

        private void ActualizarEtiquetasPeriodo()
        {
            var periodo = PeriodSelector.SelectedItem?.ToString() ?? "Meses";

            // Calcular totales del período seleccionado
            var ventasPeriodo = CalcularTotalPeriodo(balance.Ventas, periodo);
            var gastosPeriodo = CalcularTotalPeriodo(balance.Gastos, periodo);
            var gananciasPeriodo = ventasPeriodo - gastosPeriodo;
            var margenPorcentaje = ventasPeriodo > 0 ? (gananciasPeriodo / ventasPeriodo) * 100 : 0;

            VentasPeriodoLabel.Text = $"{ventasPeriodo:C}";
            GastosPeriodoLabel.Text = $"{gastosPeriodo:C}";
            GananciasPeriodoLabel.Text = $"{gananciasPeriodo:C}";
            MargenLabel.Text = $"{margenPorcentaje:F1}%";
        }

        private decimal CalcularTotalPeriodo(List<Ventas> ventas, string periodo)
        {
            var fechaLimite = periodo switch
            {
                "Días" => DateTime.Today.AddDays(-7),
                "Semanas" => DateTime.Today.AddDays(-42), // 6 semanas
                "Meses" => DateTime.Today.AddMonths(-6),
                _ => DateTime.Today.AddMonths(-6)
            };

            return ventas.Where(v => v.Fecha >= fechaLimite)
                        .Sum(v => v.Precio * v.Cantidad);
        }

        private decimal CalcularTotalPeriodo(List<Gasto> gastos, string periodo)
        {
            var fechaLimite = periodo switch
            {
                "Días" => DateTime.Today.AddDays(-7),
                "Semanas" => DateTime.Today.AddDays(-42),
                "Meses" => DateTime.Today.AddMonths(-6),
                _ => DateTime.Today.AddMonths(-6)
            };

            return gastos.Where(g => g.Fecha >= fechaLimite)
                        .Sum(g => g.Monto * g.Cantidad);
        }

        #endregion

        #region Eventos de Navegación

        private async void InAgregarEncargo(object sender, EventArgs e)
        {
            try
            {
                var encargoModal = new EncModal(this);
                await Navigation.PushModalAsync(encargoModal);
                var nuevoEncargo = encargoModal.Encargo;

                if (nuevoEncargo != null)
                {
                    await App.DataRepo.SaveEncargoAsync(nuevoEncargo);
                    balance.Encargos.Add(nuevoEncargo);
                    ActualizarEtiquetaEncargos();
                    ActualizarEtiquetaGanancias();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo agregar el encargo: {ex.Message}", "OK");
            }
        }

        private async void InAgregarVenta(object sender, EventArgs e)
        {
            try
            {
                var ventaModal = new VentaModal(this);
                await Navigation.PushModalAsync(ventaModal);
                var nuevaVenta = ventaModal.Venta;

                if (nuevaVenta != null)
                {
                    await App.DataRepo.SaveVentasAsync(nuevaVenta);
                    balance.Ventas.Add(nuevaVenta);

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        ActualizarEtiquetaVentas();
                        ActualizarEtiquetaGanancias();
                        ActualizarEtiquetasPeriodo();
                    });

                    await ActualizarGraficosAsync();
                }
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
                var gastoModal = new GastoModal(this);
                await Navigation.PushModalAsync(gastoModal);
                var nuevoGasto = gastoModal.Gasto;

                if (nuevoGasto != null)
                {
                    await App.DataRepo.SaveGastoAsync(nuevoGasto);
                    balance.Gastos.Add(nuevoGasto);

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        ActualizarEtiquetaGastos();
                        ActualizarEtiquetaGanancias();
                        ActualizarEtiquetasPeriodo();
                    });

                    await ActualizarGraficosAsync();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo agregar el gasto: {ex.Message}", "OK");
            }
        }

        #endregion

        #region Gestión de Gráficos

        private async void PeriodSelector_OnSelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ActualizarEtiquetasPeriodo();
                });

                await ActualizarGraficosAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cambiar período: {ex.Message}");
            }
        }

        private Task ActualizarGraficosAsync()
        {
            try
            {
                var periodo = PeriodSelector.SelectedItem?.ToString() ?? "Meses";
                var ventas = balance.Ventas ?? new List<Ventas>();
                var gastos = balance.Gastos ?? new List<Gasto>();

                var ventasAgrupadas = periodo switch
                {
                    "Días" => AgruparVentasPorDia(ventas),
                    "Semanas" => AgruparVentasPorSemana(ventas),
                    _ => AgruparVentasPorMes(ventas)
                };

                var gastosAgrupados = periodo switch
                {
                    "Días" => AgruparGastosPorDia(gastos),
                    "Semanas" => AgruparGastosPorSemana(gastos),
                    _ => AgruparGastosPorMes(gastos)
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
                Console.WriteLine($"Error al actualizar gráficos: {ex}");
            }

            return Task.CompletedTask;
        }

        #endregion

        #region Métodos de Agrupación - Ventas

        private List<(string Periodo, decimal Total)> AgruparVentasPorDia(List<Ventas> ventas)
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

        private List<(string Periodo, decimal Total)> AgruparVentasPorSemana(List<Ventas> ventas)
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

        private List<(string Periodo, decimal Total)> AgruparVentasPorMes(List<Ventas> ventas)
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
                return (mes.ToString("MMM", new CultureInfo("es-ES")), total);
            }).ToList();
        }

        #endregion

        #region Métodos de Agrupación - Gastos

        private List<(string Periodo, decimal Total)> AgruparGastosPorDia(List<Gasto> gastos)
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

        private List<(string Periodo, decimal Total)> AgruparGastosPorSemana(List<Gasto> gastos)
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

        private List<(string Periodo, decimal Total)> AgruparGastosPorMes(List<Gasto> gastos)
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
                return (mes.ToString("MMM", new CultureInfo("es-ES")), total);
            }).ToList();
        }

        #endregion

        #region Configuración de Gráficos

        private void ConfigurarGraficoVentas(List<(string Periodo, decimal Total)> datos, string periodo)
        {
            try
            {
                var entries = datos.Select(d => new ChartEntry((float)d.Total)
                {
                    Label = d.Periodo,
                    ValueLabel = FormatearValorEntero(d.Total),
                    Color = SKColor.Parse("#2e9449"),
                    TextColor = SKColor.Parse("#E0E0E0"),
                    ValueLabelColor = SKColor.Parse("#FFFFFF")
                }).ToArray();

                VentasChart.Chart = new LineChart
                {
                    Entries = entries,
                    LabelTextSize = 14,
                    ValueLabelTextSize = 12,
                    BackgroundColor = SKColor.Parse("#2a2a2a"),
                    LineSize = 3,
                    PointSize = 8,
                    IsAnimated = true,
                    AnimationDuration = TimeSpan.FromMilliseconds(600),
                    LabelOrientation = Orientation.Horizontal,
                    ValueLabelOrientation = Orientation.Horizontal,
                    Margin = 20,
                    ShowYAxisLines = true,
                    YAxisLinesPaint = new SKPaint { Color = SKColor.Parse("#3C3C3C"), StrokeWidth = 1 }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error configurando gráfico de ventas: {ex.Message}");
            }
        }

        private void ConfigurarGraficoGastos(List<(string Periodo, decimal Total)> datos, string periodo)
        {
            try
            {
                var entries = datos.Select(d => new ChartEntry((float)d.Total)
                {
                    Label = d.Periodo,
                    ValueLabel = FormatearValorEntero(d.Total),
                    Color = SKColor.Parse("#d63384"),
                    TextColor = SKColor.Parse("#E0E0E0"),
                    ValueLabelColor = SKColor.Parse("#FFFFFF")
                }).ToArray();

                GastosChart.Chart = new LineChart
                {
                    Entries = entries,
                    LabelTextSize = 14,
                    ValueLabelTextSize = 12,
                    BackgroundColor = SKColor.Parse("#2a2a2a"),
                    LineSize = 3,
                    PointSize = 8,
                    IsAnimated = true,
                    AnimationDuration = TimeSpan.FromMilliseconds(600),
                    LabelOrientation = Orientation.Horizontal,
                    ValueLabelOrientation = Orientation.Horizontal,
                    Margin = 20,
                    ShowYAxisLines = true,
                    YAxisLinesPaint = new SKPaint { Color = SKColor.Parse("#3C3C3C"), StrokeWidth = 1 }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error configurando gráfico de gastos: {ex.Message}");
            }
        }

        private void ConfigurarGraficoGanancias(List<(string Periodo, decimal Total)> ventas,
                                               List<(string Periodo, decimal Total)> gastos,
                                               string periodo)
        {
            try
            {
                var datosCompletos = ventas.Select(v =>
                {
                    var gastoCorrespondiente = gastos.FirstOrDefault(g => g.Periodo == v.Periodo);
                    var ganancia = v.Total - gastoCorrespondiente.Total;
                    return new { Periodo = v.Periodo, Ganancia = ganancia };
                }).ToList();

                var entries = datosCompletos.Select(d => new ChartEntry((float)d.Ganancia)
                {
                    Label = d.Periodo,
                    ValueLabel = FormatearValorEntero(d.Ganancia),
                    Color = d.Ganancia >= 0 ? SKColor.Parse("#1f6bc2") : SKColor.Parse("#dc3545"),
                    TextColor = SKColor.Parse("#E0E0E0"),
                    ValueLabelColor = SKColor.Parse("#FFFFFF")
                }).ToArray();

                GananciasChart.Chart = new LineChart
                {
                    Entries = entries,
                    LabelTextSize = 14,
                    ValueLabelTextSize = 12,
                    BackgroundColor = SKColor.Parse("#2a2a2a"),
                    LineSize = 3,
                    PointSize = 8,
                    IsAnimated = true,
                    AnimationDuration = TimeSpan.FromMilliseconds(600),
                    LabelOrientation = Orientation.Horizontal,
                    ValueLabelOrientation = Orientation.Horizontal,
                    Margin = 20,
                    ShowYAxisLines = true,
                    YAxisLinesPaint = new SKPaint { Color = SKColor.Parse("#3C3C3C"), StrokeWidth = 1 }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error configurando gráfico de ganancias: {ex.Message}");
            }
        }

        #endregion

        #region Métodos Auxiliares

        private string FormatearValorEntero(decimal valor)
        {
            if (Math.Abs(valor) >= 1000000)
                return $"${Math.Round(valor / 1000000, 1)}M";
            else if (Math.Abs(valor) >= 1000)
                return $"${Math.Round(valor / 1000, 1)}K";
            else if (valor == 0)
                return "$0";
            else
                return $"${Math.Round(valor)}";
        }

        #endregion

        #region Exportación a Excel

        private async void OnExportarAExcelClicked(object sender, EventArgs e)
        {
            try
            {
                // Deshabilitar el botón temporalmente para evitar múltiples clicks
                var botonExportar = sender as Button;
                if (botonExportar != null)
                {
                    botonExportar.IsEnabled = false;
                    botonExportar.Text = "⏳ Exportando...";
                }

                string carpetaPersonalizada = Path.Combine(FileSystem.Current.AppDataDirectory, "Exportaciones");

                if (!Directory.Exists(carpetaPersonalizada))
                {
                    Directory.CreateDirectory(carpetaPersonalizada);
                }

                string nombreArchivo = $"Balance_Financiero_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                string rutaArchivo = Path.Combine(carpetaPersonalizada, nombreArchivo);

                // Asegurar que los datos estén actualizados
                await CargarDatosAsync();

                // Exportar con el nuevo formato
                await ExportExcel.ExportarBalanceAExcelAsync(balance, rutaArchivo);

                var mensaje = $"📊 ¡Reporte generado exitosamente!\n\n" +
                             $"📁 Archivo: {nombreArchivo}\n" +
                             $"📍 Ubicación: {carpetaPersonalizada}\n\n" +
                             $"✨ El reporte incluye:\n" +
                             $"• Resumen ejecutivo con métricas clave\n" +
                             $"• Análisis mensual detallado\n" +
                             $"• Registros completos por categoría\n" +
                             $"• Datos listos para gráficos";

                var respuesta = await DisplayAlert(
                    "✅ Exportación Completada",
                    mensaje,
                    "📂 Abrir archivo",
                    "✋ Cerrar"
                );

                if (respuesta)
                {
                    await AbrirUbicacionArchivoAsync(rutaArchivo, carpetaPersonalizada);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "❌ Error en la exportación",
                    $"No se pudo generar el reporte Excel:\n\n{ex.Message}",
                    "Entendido"
                );
                Console.WriteLine($"Error detallado en exportación: {ex}");
            }
            finally
            {
                // Rehabilitar el botón
                var botonExportar = sender as Button;
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

        #endregion
    }
}