namespace Mercader
{
    public partial class AppShell : Shell
    {
        public AppShell(ShellItem mainPage)
        {
            InitializeComponent();
            // Configurar MainPage como página principal
            Routing.RegisterRoute("MainPage", typeof(MainPage));
            Routing.RegisterRoute("Encargos", typeof(Encargos));

            // Establecer MainPage como la página raíz
            


        }
    }
}
