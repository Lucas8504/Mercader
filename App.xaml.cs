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

            // Inicializar SQLite primero
            SQLitePCL.Batteries_V2.Init();

            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "MercaderDB.db3");
            _dataRepo = new(dbPath);

            // Establecer MainPage antes de la inicialización de la base de datos
            MainPage = new AppShell();

            // Inicializar la base de datos después de establecer MainPage
            InitializeDatabaseAsync().ConfigureAwait(false);
        }

        private async Task InitializeDatabaseAsync()
        {
            try
            {
                await _dataRepo!.InitializeDatabaseAsync();

                // Cargar datos después de inicializar la base de datos
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await LoadDataAsync();
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en inicialización: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Current!.MainPage!.DisplayAlert("Error",
                        $"Error al inicializar la base de datos: {ex.Message}",
                        "OK");
                });
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
                
                    if (MainPage is AppShell appShell &&
               appShell.CurrentPage is MainPage mainPage)
                    {
                        mainPage.balance.Encargos = encargos;
                        mainPage.balance.Gastos = gastos;
                        mainPage.balance.Ventas = ventas;

                        mainPage.ActualizarEtiquetaEncargos();
                        mainPage.ActualizarEtiquetaGastos();
                        mainPage.ActualizarEtiquetaVentas();
                        mainPage.ActualizarEtiquetaGanancias();
                    }
                
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al cargar datos: {ex.Message}");
                await Current!.MainPage!.DisplayAlert("Error",
                    $"Error al cargar datos: {ex.Message}",
                    "OK");
            }
        }
    }
}

