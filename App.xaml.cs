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

            try
            {
                InitializeComponent();
                string dbPath = Path.Combine(FileSystem.AppDataDirectory, "MercaderDB.db3");
                _dataRepo = new(dbPath);
                InitializeDatabase(); // Llamamos al método de inicialización
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
            }).ConfigureAwait(false);
        }
    }
}

