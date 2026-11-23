using CommunityToolkit.Maui.Storage;
using Mercader.Core.Services.Localization;
using Mercader.Services.Interfaces;
using Mercader.ViewModels;
using Microcharts;
using SkiaSharp;

namespace Mercader
{
    public partial class MainPage : ContentPage
    {
        private readonly MainViewModel _viewModel;
        private readonly IExportPdfService _pdfService;
        private readonly IBalanceCalculatorService _balanceCalculator;
        private readonly IChartService _chartService;

        public MainPage(
            MainViewModel vm,
            IExportPdfService pdfService,
            IBalanceCalculatorService balanceCalculator,
            IChartService chartService)
        {
            InitializeComponent();
            _viewModel = vm;
            _pdfService = pdfService;
            _balanceCalculator = balanceCalculator;
            _chartService = chartService;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] MainPage.OnAppearing: {ex}");
            }
        }

        #region Eventos de Navegación

        private async void InAgregarEncargo(object sender, EventArgs e)
        {
            try
            {
                await _viewModel.AgregarEncargoCommand.ExecuteAsync(null);
            }
            catch (Exception ex)
            {
                await DisplayAlert(Loc.Get("Error"), Loc.Format("NoSePudoAbrirFormulario", ex.Message), "OK");
            }
        }

        private async void InAgregarVenta(object sender, EventArgs e)
        {
            try
            {
                await _viewModel.AgregarVentaCommand.ExecuteAsync(null);
            }
            catch (Exception ex)
            {
                await DisplayAlert(Loc.Get("Error"), Loc.Format("NoSePudoAgregarVenta", ex.Message), "OK");
            }
        }

        private async void InAgregarGasto(object sender, EventArgs e)
        {
            try
            {
                await _viewModel.AgregarGastoCommand.ExecuteAsync(null);
            }
            catch (Exception ex)
            {
                await DisplayAlert(Loc.Get("Error"), Loc.Format("NoSePudoAbrirModal", ex.Message), "OK");
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

        /// <summary>
        /// Renders a Microcharts Chart to a PNG byte array at the specified resolution.
        /// </summary>
        /// <summary>
        /// Renders a Microcharts Chart to a PNG byte array.
        /// Forces full animation progress so the chart renders at final state.
        /// </summary>
        private static byte[]? RenderChartToPng(Chart chart, int width, int height)
        {
            if (chart == null) return null;

            // Save original state so we can restore it (avoid mutating the shared chart object)
            bool originalIsAnimated = chart.IsAnimated;
            float originalProgress = chart.AnimationProgress;

            // Force full render — without this, a fresh chart renders flat (AnimationProgress = 0)
            chart.IsAnimated = false;
            chart.AnimationProgress = 1;

            var info = new SKImageInfo(width, height);
            using var surface = SKSurface.Create(info);
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);
            chart.Draw(canvas, width, height);

            // Restore original animation state so the on-screen chart isn't broken
            chart.IsAnimated = originalIsAnimated;
            chart.AnimationProgress = originalProgress;

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 90);
            return data.ToArray();
        }

        #endregion

        #region Exportación

        /// <summary>
        /// Toggles exporting visual state for the button that triggered the
        /// export. Disables both buttons but only animates the active one.
        /// </summary>
        private async Task SetExportingState(Button activeButton, bool exporting)
        {
            bool isExcel = activeButton == BtnExcel;

            BtnExcel.IsEnabled = !exporting;
            BtnPdf.IsEnabled   = !exporting;

            var spinner = isExcel ? SpinnerExcel : SpinnerPdf;

            spinner.IsRunning = exporting;
            spinner.IsVisible = exporting;

            if (exporting)
                await activeButton.FadeTo(0.65, 200, Easing.CubicIn);
            else
                await activeButton.FadeTo(1.0, 250, Easing.CubicOut);
        }

        private async void OnExportarPdfClicked(object sender, EventArgs e)
        {
            await SetExportingState(BtnPdf, true);
            try
            {
            // — Diagnóstico: log de lo que pasa durante la generación —
            var diag = new List<string>();
            diag.Add($"--- DIAG PDF {DateTime.Now:HH:mm:ss} ---");
            diag.Add($"Periodo VM: {_viewModel.PeriodoSeleccionado}");
            diag.Add($"Ventas count: {_viewModel.Ventas?.Count ?? -1}");
            diag.Add($"Gastos count: {_viewModel.Gastos?.Count ?? -1}");
            diag.Add($"GananciasChart is null: {_viewModel.GananciasChart == null}");
                await _viewModel.CargarDatosCommand.ExecuteAsync(null);

                diag.Add("--- Tras CargarDatos ---");
                diag.Add($"Ventas count: {_viewModel.Ventas?.Count ?? -1}");
                diag.Add($"Gastos count: {_viewModel.Gastos?.Count ?? -1}");
                diag.Add($"GananciasChart is null: {_viewModel.GananciasChart == null}");
                diag.Add($"Periodo VM: {_viewModel.PeriodoSeleccionado}");

                string nombreArchivo = $"Balance_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var exportData = await _viewModel.CrearExportDtoAsync();

                // Font: OpenSans para caracteres Unicode
                byte[]? fontBytes = null;
                try
                {
                    using var fontStream = await FileSystem.OpenAppPackageFileAsync("OpenSans-Regular.ttf");
                    using var ms = new MemoryStream();
                    await fontStream.CopyToAsync(ms);
                    fontBytes = ms.ToArray();
                    diag.Add($"Font loaded: {fontBytes.Length} bytes");
                }
                catch (Exception exFont)
                {
                    diag.Add($"Font fallback: {exFont.Message}");
                }

                // Charts
                var charts = new List<(byte[] ImageBytes, string Title)>();

                // Helper: devuelve los últimos N períodos de ventas y gastos
                (List<(string Periodo, decimal Total)> ventas, List<(string Periodo, decimal Total)> gastos) GetLatest(string periodoType, int count)
                {
                    var v = _balanceCalculator.AgruparVentasPorPeriodo(_viewModel.Ventas, periodoType)
                        .TakeLast(count).ToList();
                    var g = _balanceCalculator.AgruparGastosPorPeriodo(_viewModel.Gastos, periodoType)
                        .TakeLast(count).ToList();
                    return (v, g);
                }

                // 1. Ventas (6 Meses y 30 Días)
                try
                {
                    var (v6, g6) = GetLatest("Meses", 6);
                    if (v6.Count > 0)
                    {
                        var chart = _chartService.CrearGraficoVentas(v6);
                        var bytes = RenderChartToPng(chart, 1400, 450);
                        if (bytes != null)
                            charts.Add((bytes, $"{Loc.Get("TituloVentas")} — {Loc.Get("Ultimos6Meses")}"));
                    }
                    var (v30, g30) = GetLatest("Días", 30);
                    if (v30.Count > 0)
                    {
                        for (int i = 0; i < v30.Count; i += 7)
                        {
                            var weekV = v30.Skip(i).Take(7).ToList();
                            if (weekV.Count == 0) continue;
                            string label = weekV.First().Periodo;
                            if (weekV.Count > 1) label = $"{weekV.First().Periodo} — {weekV.Last().Periodo}";
                            var chart = _chartService.CrearGraficoVentas(weekV);
                            var bytes = RenderChartToPng(chart, 1400, 450);
                            if (bytes != null)
                                charts.Add((bytes, $"{Loc.Get("TituloVentas")} — {Loc.Get("Ultimos30Dias")} — {label}"));
                        }
                    }
                }
                catch (Exception ex) { diag.Add($"Ventas chart exception: {ex.Message}"); }

                // 2. Gastos (6 Meses y 30 Días)
                try
                {
                    var (v6, g6) = GetLatest("Meses", 6);
                    if (g6.Count > 0)
                    {
                        var chart = _chartService.CrearGraficoGastos(g6);
                        var bytes = RenderChartToPng(chart, 1400, 450);
                        if (bytes != null)
                            charts.Add((bytes, $"{Loc.Get("TituloGastos")} — {Loc.Get("Ultimos6Meses")}"));
                    }
                    var (v30, g30) = GetLatest("Días", 30);
                    if (g30.Count > 0)
                    {
                        for (int i = 0; i < g30.Count; i += 7)
                        {
                            var weekG = g30.Skip(i).Take(7).ToList();
                            if (weekG.Count == 0) continue;
                            string label = weekG.First().Periodo;
                            if (weekG.Count > 1) label = $"{weekG.First().Periodo} — {weekG.Last().Periodo}";
                            var chart = _chartService.CrearGraficoGastos(weekG);
                            var bytes = RenderChartToPng(chart, 1400, 450);
                            if (bytes != null)
                                charts.Add((bytes, $"{Loc.Get("TituloGastos")} — {Loc.Get("Ultimos30Dias")} — {label}"));
                        }
                    }
                }
                catch (Exception ex) { diag.Add($"Gastos chart exception: {ex.Message}"); }

                // 3. Encargos (6 Meses y 30 Días)
                try
                {
                    var encMeses = _balanceCalculator.AgruparEncargosPorPeriodo(_viewModel.Encargos, "Meses").TakeLast(6).ToList();
                    if (encMeses.Count > 0)
                    {
                        var chart = _chartService.CrearGraficoEncargos(encMeses);
                        var bytes = RenderChartToPng(chart, 1400, 450);
                        if (bytes != null)
                            charts.Add((bytes, $"{Loc.Get("TituloEncargos")} — {Loc.Get("Ultimos6Meses")}"));
                    }
                    var encDias = _balanceCalculator.AgruparEncargosPorPeriodo(_viewModel.Encargos, "Días").TakeLast(30).ToList();
                    if (encDias.Count > 0)
                    {
                        for (int i = 0; i < encDias.Count; i += 7)
                        {
                            var weekE = encDias.Skip(i).Take(7).ToList();
                            if (weekE.Count == 0) continue;
                            string label = weekE.First().Periodo;
                            if (weekE.Count > 1) label = $"{weekE.First().Periodo} — {weekE.Last().Periodo}";
                            var chart = _chartService.CrearGraficoEncargos(weekE);
                            var bytes = RenderChartToPng(chart, 1400, 450);
                            if (bytes != null)
                                charts.Add((bytes, $"{Loc.Get("TituloEncargos")} — {Loc.Get("Ultimos30Dias")} — {label}"));
                        }
                    }
                }
                catch (Exception ex) { diag.Add($"Encargos chart exception: {ex.Message}"); }

                // 4. Ganancias (6 Meses y 30 Días)
                try
                {
                    var (v6, g6) = GetLatest("Meses", 6);
                    if (v6.Count > 0)
                    {
                        var chart = _chartService.CrearGraficoGanancias(v6, g6);
                        var bytes = RenderChartToPng(chart, 1400, 450);
                        if (bytes != null)
                            charts.Add((bytes, $"{Loc.Get("TituloGanancias")} — {Loc.Get("Ultimos6Meses")}"));
                    }
                    var (v30, g30) = GetLatest("Días", 30);
                    if (v30.Count > 0)
                    {
                        for (int i = 0; i < v30.Count; i += 7)
                        {
                            var weekV = v30.Skip(i).Take(7).ToList();
                            var weekG = g30.Skip(i).Take(7).ToList();
                            if (weekV.Count == 0) continue;
                            string label = weekV.First().Periodo;
                            if (weekV.Count > 1) label = $"{weekV.First().Periodo} — {weekV.Last().Periodo}";
                            var chart = _chartService.CrearGraficoGanancias(weekV, weekG);
                            var bytes = RenderChartToPng(chart, 1400, 450);
                            if (bytes != null)
                                charts.Add((bytes, $"{Loc.Get("TituloGanancias")} — {Loc.Get("Ultimos30Dias")} — {label}"));
                        }
                    }
                }
                catch (Exception ex) { diag.Add($"Ganancias chart exception: {ex.Message}"); }

                diag.Add($"--- Total charts a pasar al PDF: {charts.Count} ---");

                using var pdfStream = _pdfService.GenerarBalancePdf(
                    exportData, charts.Count > 0 ? charts : null, fontBytes);

                var saverResult = await FileSaver.Default.SaveAsync(
                    nombreArchivo, pdfStream, CancellationToken.None);

                // — Guardar diagnóstico —
                try
                {
                    string diagDir = Path.Combine(FileSystem.CacheDirectory, "MercaderDiag");
                    Directory.CreateDirectory(diagDir);
                    string diagPath = Path.Combine(diagDir, $"pdf_diag_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                    await File.WriteAllTextAsync(diagPath, string.Join(Environment.NewLine, diag));

                    if (saverResult.IsSuccessful)
                        await DisplayAlert(Loc.Get("PdfExportado"),
                            $"{Loc.Get("BalanceGuardado")}\n\n{Loc.Format("Diagnostico", diagPath)}", "OK");
                    else
                        await DisplayAlert("PDF", Loc.Format("PdfNoGuardado", diagPath), "OK");
                }
                catch
                {
                    if (saverResult.IsSuccessful)
                        await DisplayAlert(Loc.Get("PdfExportado"), Loc.Get("BalanceGuardado"), "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert(Loc.Get("Error"), Loc.Format("NoSePudoExportarPdf", ex.Message), "OK");
                System.Diagnostics.Debug.WriteLine($"[PDF] Error: {ex}");
            }
            finally
            {
                await SetExportingState(BtnPdf, false);
            }
        }

        private async void OnExportarAExcelClicked(object sender, EventArgs e)
        {
            await SetExportingState(BtnExcel, true);
            try
            {
                await _viewModel.CargarDatosCommand.ExecuteAsync(null);

                string nombreArchivo = $"Balance_Financiero_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                var exportData = await _viewModel.CrearExportDtoAsync();

                string tempDir = Path.Combine(FileSystem.Current.CacheDirectory, "Exportaciones");
                Directory.CreateDirectory(tempDir);
                string rutaTemp = Path.Combine(tempDir, nombreArchivo);
                await ExportExcel.ExportarBalanceAExcelAsync(exportData, rutaTemp);

                using var fileStream = File.OpenRead(rutaTemp);
 
#if IOS || MACCATALYST
                if (OperatingSystem.IsIOSVersionAtLeast(14) || OperatingSystem.IsMacCatalystVersionAtLeast(14))
                {
                    var saverResult = await FileSaver.Default.SaveAsync(nombreArchivo, fileStream, CancellationToken.None);
                    await HandleFileSaverResult(saverResult, rutaTemp, nombreArchivo);
                }
                else
                {
                    throw new PlatformNotSupportedException(Loc.Get("ExportSoloIos"));
                }
#else
                var saverResult = await FileSaver.Default.SaveAsync(nombreArchivo, fileStream, CancellationToken.None);
                await HandleFileSaverResult(saverResult, rutaTemp, nombreArchivo);
#endif
            }
            catch (Exception ex)
            {
                var mensajeError = Loc.Format("NoSePudoGenerarReporte", ex.Message);
#if DEBUG
                mensajeError += $"\n\n📋 {ex.GetType().Name}: {ex.StackTrace?.Split('\n').FirstOrDefault()}";
#endif
                await DisplayAlert(Loc.Get("ErrorEnExportacion"), mensajeError, Loc.Get("Entendido"));
                System.Diagnostics.Debug.WriteLine($"[ExportError] {ex}");
            }
            finally
            {
                await SetExportingState(BtnExcel, false);
            }
        }

        private async Task HandleFileSaverResult(FileSaverResult saverResult, string rutaTemp, string nombreArchivo)
        {
            string rutaFinal;
            if (saverResult.IsSuccessful)
            {
                var rutaGuardado = saverResult.FilePath;
                File.Delete(rutaTemp);
                rutaFinal = $"📁 {rutaGuardado ?? Loc.Get("UbicacionElegida")}";
            }
            else
            {
#if ANDROID
                (rutaFinal, _) = await GuardarEnDescargasAndroidAsync(rutaTemp, nombreArchivo);
#else
                string carpetaDocs = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Mercader");
                Directory.CreateDirectory(carpetaDocs);
                rutaFinal = Path.Combine(carpetaDocs, nombreArchivo);
                File.Move(rutaTemp, rutaFinal, overwrite: true);
#endif
            }

            var mensaje = Loc.Get("ReporteGenerado") +
                          $"📁 {nombreArchivo}\n" +
                          $"📍 {rutaFinal}";

            await DisplayAlert(Loc.Get("ExportacionCompletada"), mensaje, "OK");
        }

#if ANDROID
        /// <summary>
        /// Guarda el archivo en Downloads/Mercader/.
        /// Android 9-10: ruta directa.
        /// Android 11+: MediaStore API (no necesita permisos especiales).
        /// </summary>
        private static async Task<(string displayPath, string? contentUri)> GuardarEnDescargasAndroidAsync(string rutaTemp, string nombreArchivo)
        {
            // Android 11+ (API 30+) → MediaStore (no necesita permisos)
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.R)
            {
                return await GuardarConMediaStoreAsync(rutaTemp, nombreArchivo);
            }

            // Android 4.4-10 → intentar ruta directa a Downloads
            try
            {
                var rutaDownloads = Android.OS.Environment.GetExternalStoragePublicDirectory(
                    Android.OS.Environment.DirectoryDownloads)?.AbsolutePath;

                if (!string.IsNullOrEmpty(rutaDownloads))
                {
                    string carpetaMercader = Path.Combine(rutaDownloads, "Mercader");
                    Directory.CreateDirectory(carpetaMercader);
                    string rutaFinal = Path.Combine(carpetaMercader, nombreArchivo);

                    File.Move(rutaTemp, rutaFinal, overwrite: true);
                    return (rutaFinal, null); // contentUri null = ruta directa de archivo
                }
            }
            catch
            {
                // Sin permiso WRITE_EXTERNAL_STORAGE, cae al fallback
            }

            // Fallback: ExternalFilesDir (funciona sin permisos en TODAS las versiones)
            // Ruta: /storage/emulated/0/Android/data/{package}/files/Documents/Mercader/
            var rutaFallback = Android.App.Application.Context.GetExternalFilesDir(
                Android.OS.Environment.DirectoryDocuments)?.AbsolutePath;

            if (string.IsNullOrEmpty(rutaFallback))
                throw new InvalidOperationException("No se pudo obtener una carpeta para guardar el archivo");

            string carpetaFallback = Path.Combine(rutaFallback, "Mercader");
            Directory.CreateDirectory(carpetaFallback);
            string archivoFinal = Path.Combine(carpetaFallback, nombreArchivo);

            File.Move(rutaTemp, archivoFinal, overwrite: true);
            return (archivoFinal, null);
        }

        /// <summary>
        /// Guarda en Downloads usando MediaStore (Android 11+).
        /// Solo se llama cuando SdkInt >= R (API 30), por eso suprimimos CA1416.
        /// </summary>
#pragma warning disable CA1416 // MediaStore.Downloads disponible desde API 29
        private static async Task<(string displayPath, string contentUri)> GuardarConMediaStoreAsync(string rutaTemp, string nombreArchivo)
        {
            byte[] bytes = await File.ReadAllBytesAsync(rutaTemp);

            var contentValues = new Android.Content.ContentValues();
            contentValues.Put(Android.Provider.MediaStore.IMediaColumns.DisplayName, nombreArchivo);
            contentValues.Put(Android.Provider.MediaStore.IMediaColumns.MimeType,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            contentValues.Put(Android.Provider.MediaStore.IMediaColumns.RelativePath, "Download/Mercader");

            var context = Android.App.Application.Context;
            var uri = context.ContentResolver?.Insert(
                Android.Provider.MediaStore.Downloads.ExternalContentUri, contentValues);

            if (uri == null)
                throw new InvalidOperationException("No se pudo crear el archivo en Descargas (MediaStore)");

            using var outputStream = context.ContentResolver!.OpenOutputStream(uri);
            if (outputStream == null)
                throw new InvalidOperationException("No se pudo abrir el stream para escribir en Descargas");

            await outputStream.WriteAsync(bytes, 0, bytes.Length);
            await outputStream.FlushAsync();

            // Limpiar temp
            File.Delete(rutaTemp);

            // Devolver ruta visible + content URI para abrir el archivo
            return ($"/storage/emulated/0/Download/Mercader/{nombreArchivo}", uri.ToString()!);
        }
#pragma warning restore CA1416
#endif

        private async Task AbrirOCompartirArchivoAsync(string rutaArchivo, string? contentUri = null)
        {
            try
            {
#if ANDROID
                // Caso 1: MediaStore → abrir con content URI vía Intent directo
                if (!string.IsNullOrEmpty(contentUri))
                {
                    var uri = Android.Net.Uri.Parse(contentUri);
                    if (uri != null)
                    {
                        var intent = new Android.Content.Intent(Android.Content.Intent.ActionView);
                        intent.SetDataAndType(uri,
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                        intent.AddFlags(Android.Content.ActivityFlags.GrantReadUriPermission);
                        Android.App.Application.Context.StartActivity(intent);
                        return;
                    }
                }

                // Caso 2: ruta directa, el archivo existe
                if (File.Exists(rutaArchivo))
                {
                    await Launcher.OpenAsync(new OpenFileRequest
                    {
                        File = new ReadOnlyFile(rutaArchivo),
                        Title = Loc.Get("BalanceFinanciero")
                    });
                    return;
                }

                // Caso 3: no encontramos el archivo
                await DisplayAlert("Info",
                    Loc.Get("ArchivoEnDescargas"),
                    "OK");
#else
                if (!File.Exists(rutaArchivo))
                {
                    await DisplayAlert(Loc.Get("Error"), Loc.Get("ArchivoNoEncontrado"), "OK");
                    return;
                }

                await Launcher.OpenAsync(new OpenFileRequest
                {
                    File = new ReadOnlyFile(rutaArchivo),
                    Title = "Balance Financiero"
                });
#endif
            }
            catch (Exception ex)
            {
                await DisplayAlert(Loc.Get("Error"), Loc.Format("NoSePudoAbrir", ex.Message), "OK");
            }
        }

        #endregion
    }
}
