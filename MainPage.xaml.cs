using CommunityToolkit.Maui.Storage;
using Mercader.ViewModels;

namespace Mercader
{
    public partial class MainPage : ContentPage
    {
        private readonly MainViewModel _viewModel;

        public MainPage(MainViewModel vm)
        {
            InitializeComponent();
            _viewModel = vm;
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
                await DisplayAlert("Error", $"No se pudo abrir el formulario: {ex.Message}", "OK");
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
                await DisplayAlert("Error", $"No se pudo agregar la venta: {ex.Message}", "OK");
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

                string nombreArchivo = $"Balance_Financiero_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                var exportData = _viewModel.CrearExportDto();

                // 1. Generar Excel a temporal
                string tempDir = Path.Combine(FileSystem.Current.CacheDirectory, "Exportaciones");
                Directory.CreateDirectory(tempDir);
                string rutaTemp = Path.Combine(tempDir, nombreArchivo);
                await ExportExcel.ExportarBalanceAExcelAsync(exportData, rutaTemp);

                // 2. FileSaver: diálogo nativo "Guardar como"
                using var fileStream = File.OpenRead(rutaTemp);
 
#if IOS || MACCATALYST
                if (OperatingSystem.IsIOSVersionAtLeast(14) || OperatingSystem.IsMacCatalystVersionAtLeast(14))
                {
                    var saverResult = await FileSaver.Default.SaveAsync(nombreArchivo, fileStream, CancellationToken.None);
                    await HandleFileSaverResult(saverResult, rutaTemp, nombreArchivo);
                }
                else
                {
                    throw new PlatformNotSupportedException("La exportación solo es compatible con iOS 14.0+ y MacCatalyst 14.0+.");
                }
#else
                var saverResult = await FileSaver.Default.SaveAsync(nombreArchivo, fileStream, CancellationToken.None);
                await HandleFileSaverResult(saverResult, rutaTemp, nombreArchivo);
#endif
            }
            catch (Exception ex)
            {
                var mensajeError = $"No se pudo generar el reporte:\n\n{ex.Message}";
#if DEBUG
                mensajeError += $"\n\n📋 {ex.GetType().Name}: {ex.StackTrace?.Split('\n').FirstOrDefault()}";
#endif
                await DisplayAlert("❌ Error en exportación", mensajeError, "Entendido");
                System.Diagnostics.Debug.WriteLine($"[ExportError] {ex}");
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

        private async Task HandleFileSaverResult(FileSaverResult saverResult, string rutaTemp, string nombreArchivo)
        {
            string rutaFinal;
            if (saverResult.IsSuccessful)
            {
                var rutaGuardado = saverResult.FilePath;
                File.Delete(rutaTemp);
                rutaFinal = $"📁 {rutaGuardado ?? "Ubicación elegida"}";
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

            var mensaje = $"📊 ¡Reporte generado!\n\n" +
                          $"📁 {nombreArchivo}\n" +
                          $"📍 {rutaFinal}";

            await DisplayAlert("✅ Exportación Completada", mensaje, "OK");
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
                        Title = "Balance Financiero"
                    });
                    return;
                }

                // Caso 3: no encontramos el archivo
                await DisplayAlert("Info",
                    "El archivo se guardó en Descargas/Mercader/. Abrí tu gestor de archivos para verlo.",
                    "OK");
#else
                if (!File.Exists(rutaArchivo))
                {
                    await DisplayAlert("Error", "No se encontró el archivo.", "OK");
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
                await DisplayAlert("Error", $"No se pudo abrir: {ex.Message}", "OK");
            }
        }

        #endregion
    }
}
