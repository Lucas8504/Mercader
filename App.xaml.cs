using System.Diagnostics;
using Mercader.Services.Interfaces;

namespace Mercader
{
    public partial class App : Application
    {
        public App(AppShell shell, IDataRepository repo)
        {
            InitializeComponent();

            SQLitePCL.Batteries_V2.Init();

            // Aplicar tema guardado
            AplicarTema();

            MainPage = shell;
        }

        public static void AplicarTema()
        {
            var tema = Preferences.Get("tema", "system");

            Current.UserAppTheme = tema switch
            {
                "light" => AppTheme.Light,
                "dark" => AppTheme.Dark,
                _ => AppTheme.Unspecified // sigue al sistema
            };
        }
    }
}
