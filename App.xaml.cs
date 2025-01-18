using System.Diagnostics;

namespace Mercader
{
    public partial class App : Application
    {
        private static DataRepository? _dataRepo;
        public static DataRepository DataRepo =>
        _dataRepo ?? throw new InvalidOperationException("DataRepo no está inicializado.");

        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validar la compatibilidad de la plataforma", Justification = "<pendiente>")]
        public App()
        {

            try
            {
                // Inicializar SQLite
                SQLitePCL.Batteries_V2.Init();

                InitializeComponent();
                string dbPath = Path.Combine(
                    FileSystem.AppDataDirectory, "MercaderDB.db3");
                _dataRepo = new(dbPath);
                InitializeDatabase();
                MainPage = new AppShell();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en la inicialización: {ex.Message}");
                throw;
            }
        }
        private void InitializeDatabase()
        {
            Task.Run(async () =>
            {
                try
                {
                    await _dataRepo!.InitializeDatabaseAsync();
                    await LoadDataAsync();
                }
                catch (Exception ex)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        if (Current?.MainPage != null)
                        {
                            await Current.MainPage.DisplayAlert("Error",
                                $"Error al inicializar la base de datos: {ex.Message}",
                                "OK");
                        }
                    });
                }
            });
        }
        private async Task LoadDataAsync()
        {
            var encargos = await _dataRepo!.GetEncargosAsync();
            var gastos = await _dataRepo.GetGastosAsync();
            var ventas = await _dataRepo.GetVentasAsync();

            // Asigna los datos cargados a la instancia de Balance
            var mainPage = MainPage as MainPage;
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

    }
}

