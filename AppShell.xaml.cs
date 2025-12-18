namespace Mercader
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            
            Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
            Routing.RegisterRoute(nameof(Gastos), typeof(Gastos));
            Routing.RegisterRoute(nameof(Encargos), typeof(Encargos));
            Routing.RegisterRoute(nameof(Venta), typeof(Venta));
        }

        protected override bool OnBackButtonPressed()
        {
            return true;
        }
    }
}
