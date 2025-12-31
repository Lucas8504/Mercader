using Microcharts.Maui;
using SkiaSharp;
using Mercader.Helpers;
using System.Globalization;
using Microcharts;
using System.Diagnostics;
using Mercader.ViewModels;

namespace Mercader
{
    public partial class MainPage : ContentPage
    {
        public ViewModels.MainViewModel VM { get; }
        
        public Balance balance;
        private readonly DataRepository _repo;

        public MainPage(DataRepository repo, ViewModels.MainViewModel vm)
        {

            InitializeComponent();

            _repo = repo;
            VM = vm;
            BindingContext = VM;
            VM.Recalcular();
            balance = new Balance();


            VM.OnPeriodoChanged += async () =>
            {
                VM.Recalcular();
                await ActualizarGraficosAsync();
            };
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

                balance.Encargos = encargos;
                balance.Gastos = gastos;
                balance.Ventas = ventas;

                // Actualizar todas las etiquetas en el hilo principal
                

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
            // Esta etiqueta se actualiza en ActualizarEtiquetasPeriodo()
            // No necesita actualización individual aquí ya que depende del período seleccionado
        }

        public void ActualizarEtiquetaVentas()
        {
            decimal ventas = balance.CalcularVentas();
            // Esta etiqueta se actualiza en ActualizarEtiquetasPeriodo()
            // No necesita actualización individual aquí ya que depende del período seleccionado
        }

        public void ActualizarEtiquetaGastos()
        {
            decimal gastos = balance.CalcularGastos();
            // Esta etiqueta se actualiza en ActualizarEtiquetasPeriodo()
            // No necesita actualización individual aquí ya que depende del período seleccionado
        }

        public void ActualizarEtiquetaEncargos()
        {
            decimal encargo = balance.CalcularEncargos();
            // Los encargos no tienen etiqueta de período en el XAML actual
            // Si necesitas mostrar el total de encargos, agrega la lógica aquí
        }


      

        #endregion

        #region Eventos de Navegación

        private async void InAgregarEncargo(object sender, EventArgs e)
        {
            try
            {
                await Navigation.PushModalAsync(
                    new EncModal(this, _repo)
                );
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Error",
                    $"No se pudo abrir el formulario de encargo: {ex.Message}",
                    "OK"
                );
            }
        }

        private async void InAgregarVenta(object sender, EventArgs e)
        {
            try
            {
                var modal = new VentaModal(this, _repo);
                await Navigation.PushModalAsync(modal);
                
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
                var modal = new GastoModal(this, _repo);
                await Navigation.PushModalAsync(modal);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo abrir el modal: {ex.Message}", "OK");
            }
        }


        #endregion

        #region Gestión de Gráficos

        private Task ActualizarGraficosAsync()
        {
            try
            {
                var periodo = VM.PeriodoSeleccionado;
                var ventas = balance.Ventas ?? new List<Ventas>();
                var gastos = balance.Gastos ?? new List<Gasto>();
                var encargos = balance.Encargos ?? new List<Encargo>();

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

                var encargosAgrupados = periodo switch
                {
                    "Días" => AgruparEncargosPorDia(encargos),
                    "Semanas" => AgruparEncargosPorSemana(encargos),
                    _ => AgruparEncargosPorMes(encargos)
                };

                ConfigurarGraficoVentas(ventasAgrupadas, periodo);
                ConfigurarGraficoGastos(gastosAgrupados, periodo);
                ConfigurarGraficoEncargos(encargosAgrupados, periodo);
                ConfigurarGraficoGanancias(ventasAgrupadas, gastosAgrupados, periodo);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al actualizar gráficos: {ex}");
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

        #region Métodos de Agrupación - Encargos

        private List<(string Periodo, decimal Total)> AgruparEncargosPorDia(List<Encargo> encargos)
        {
            var hoy = DateTime.Today;
            var ultimosDias = Enumerable.Range(0, 7)
                .Select(i => hoy.AddDays(-i))
                .Reverse()
                .ToList();

            return ultimosDias.Select(fecha =>
            {
                var encargosDia = encargos.Where(e => e.Fecha.Date == fecha.Date);
                var total = encargosDia.Sum(e => e.Precio * e.Cantidad);
                return (fecha.ToString("dd/MM"), total);
            }).ToList();
        }

        private List<(string Periodo, decimal Total)> AgruparEncargosPorSemana(List<Encargo> encargos)
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
                var encargosSemana = encargos.Where(e => e.Fecha.Date >= semana.Inicio && e.Fecha.Date <= semana.Fin);
                var total = encargosSemana.Sum(e => e.Precio * e.Cantidad);
                return ($"{semana.Inicio:dd/MM}", total);
            }).ToList();
        }

        private List<(string Periodo, decimal Total)> AgruparEncargosPorMes(List<Encargo> encargos)
        {
            var hoy = DateTime.Today;
            var ultimosMeses = Enumerable.Range(0, 6)
                .Select(i => hoy.AddMonths(-i))
                .Reverse()
                .ToList();

            return ultimosMeses.Select(mes =>
            {
                var encargosMes = encargos.Where(e => e.Fecha.Year == mes.Year && e.Fecha.Month == mes.Month);
                var total = encargosMes.Sum(e => e.Precio * e.Cantidad);
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
                    LabelTextSize = 24,
                    ValueLabelTextSize = 26,
                    BackgroundColor = SKColor.Parse("#2a2a2a"),
                    LineSize = 4,
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
                    Color = SKColor.Parse("#6e0a24"),
                    TextColor = SKColor.Parse("#E0E0E0"),
                    ValueLabelColor = SKColor.Parse("#FFFFFF")
                }).ToArray();

                GastosChart.Chart = new LineChart
                {
                    Entries = entries,
                    LabelTextSize = 24,
                    ValueLabelTextSize = 26,
                    BackgroundColor = SKColor.Parse("#2a2a2a"),
                    LineSize = 4,
                    PointSize = 8,
                    IsAnimated = true,
                    AnimationDuration = TimeSpan.FromMilliseconds(500),
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

        private void ConfigurarGraficoEncargos(List<(string Periodo, decimal Total)> datos, string periodo)
        {
            try
            {
                var entries = datos.Select(d => new ChartEntry((float)d.Total)
                {
                    Label = d.Periodo,
                    ValueLabel = FormatearValorEntero(d.Total),
                    Color = SKColor.Parse("#ff6b35"),
                    TextColor = SKColor.Parse("#E0E0E0"),
                    ValueLabelColor = SKColor.Parse("#FFFFFF")
                }).ToArray();

                EncargosChart.Chart = new LineChart
                {
                    Entries = entries,
                    LabelTextSize = 24,
                    ValueLabelTextSize = 26,
                    BackgroundColor = SKColor.Parse("#2a2a2a"),
                    LineSize = 4,
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
                Console.WriteLine($"Error configurando gráfico de encargos: {ex.Message}");
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
                    LabelTextSize = 24,
                    ValueLabelTextSize = 26,
                    BackgroundColor = SKColor.Parse("#2a2a2a"),
                    LineSize = 4,
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