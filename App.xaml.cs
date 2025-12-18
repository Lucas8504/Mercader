using System.Diagnostics;

namespace Mercader
{
    public partial class App : Application
    {
        private static DataRepository? _dataRepo;
        public static DataRepository DataRepo =>
            _dataRepo ?? throw new InvalidOperationException("DataRepo no está inicializado.");

        public App(AppShell shell, DataRepository repo)
        {
            InitializeComponent();

            SQLitePCL.Batteries_V2.Init();

            Task.Run(async () =>
            {
                await repo.InitializeDatabaseAsync();
            });

            MainPage = shell;
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