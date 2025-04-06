namespace Mercader
{
    public partial class AppShell : Shell
    {
        public AppShell(ShellItem mainPage)
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
            CurrentItem = mainPage; // Inicializar MainPage
            Routing.RegisterRoute(nameof(Encargos), typeof(Encargos));
        }
    }
}
