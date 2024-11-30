namespace Mercader
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            
        }
        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Configura tu ventana principal aquí
            var window = new Window(new AppShell());
            return window;
        }

    }
}
