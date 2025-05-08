namespace Mercader
{
    public partial class AppShell : Shell
    {
        public AppShell(ShellItem mainPage)
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(Encargos), typeof(Encargos));


        }
    }
}
