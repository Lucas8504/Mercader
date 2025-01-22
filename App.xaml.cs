using System.Diagnostics;

namespace Mercader
{
    public partial class App : Application
    {
        private static DataRepository? _dataRepo;
        public static DataRepository DataRepo =>
        _dataRepo ?? throw new InvalidOperationException("DataRepo no está inicializado.");

        public App()
        {
            InitializeComponent();
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "MercaderDB.db3");
            _dataRepo = new(dbPath);

            // Inicializar de forma asíncrona
            MainThread.BeginInvokeOnMainThread(async () => {
                await InitializeDatabaseAsync();
                MainPage = new AppShell();
            });
        }

        private async Task InitializeDatabaseAsync()
        {
            try
            {
                await _dataRepo!.InitializeDatabaseAsync();
                await LoadDataAsync();
            }
            catch (Exception ex)
            { 
                Debug.WriteLine($"Error: {ex.Message}");
                if (Current?.MainPage != null)
                {
                    await Current.MainPage.DisplayAlert("Error",
                        $"Error de inicialización: {ex.Message}", "OK");
                }
            }
            
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var encargos = await _dataRepo!.GetEncargosAsync();
                var gastos = await _dataRepo.GetGastosAsync();
                var ventas = await _dataRepo.GetVentasAsync();

                // Asigna los datos cargados a la instancia de Balance
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (MainPage is AppShell appShell)
                    {
                        var mainPage = appShell.CurrentPage as MainPage;
                        if (mainPage != null)
                        {
                            mainPage.balance.Encargos = encargos;
                            mainPage.balance.Gastos = gastos;
                            mainPage.balance.Ventas = ventas;

                            // Actualiza las etiquetas
                            mainPage.ActualizarEtiquetaEncargos();
                            mainPage.ActualizarEtiquetaGastos();
                            mainPage.ActualizarEtiquetaVentas();
                            mainPage.ActualizarEtiquetaGanancias();
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al cargar datos: {ex.Message}");
                throw;
            }
        }
    }
}

