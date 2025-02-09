namespace Mercader
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
            CurrentItem = new MainPage(); // Inicializar MainPage
            Routing.RegisterRoute(nameof(Encargos), typeof(Encargos));
        }
    }
}
