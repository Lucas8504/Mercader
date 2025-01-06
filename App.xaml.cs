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
            
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "MercaderDB.db3");
            _dataRepo = new(dbPath); // Target-typed new en C# 9+

            MainThread.BeginInvokeOnMainThread(InitializeDatabaseAsync);
            MainPage = new AppShell();
        }
        private async void InitializeDatabaseAsync()
        {
            try
            {
                await DataRepo.InitializeDatabaseAsync();

            }
            catch (Exception ex)
            {
                // Manejar cualquier error de inicialización
                if (Current?.MainPage != null)
                {
                    await Current.MainPage.DisplayAlert("Error",
                        "Error al inicializar la base de datos: " + ex.Message, "OK");
                }
            }
        }
    }
}


