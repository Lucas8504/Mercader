namespace Mercader
{
    public partial class App : Application
    {
        private static DataRepository? _dataRepo;
        public static DataRepository DataRepo =>
        _dataRepo ?? throw new InvalidOperationException("DataRepo no está inicializado.");

        public App(DataRepository repo)
        {

            _dataRepo = repo ?? throw new ArgumentNullException(nameof(repo));

            InitializeComponent();
            MainPage = new AppShell();

            MainThread.BeginInvokeOnMainThread(InitializeDatabaseAsync);

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


