using System.Diagnostics;
using Mercader.Services.Interfaces;
using Mercader.Services.Localization;

namespace Mercader
{
    public partial class App : Application
    {
        public App(IDataRepository repo)
        {
            InitializeComponent();

            SQLitePCL.Batteries_V2.Init();

            // Aplicar tema guardado
            AplicarTema();

            // Cargar catálogos de idioma y aplicar el idioma guardado ANTES de construir
            // el Shell. El XAML del Shell se parsea en su constructor y las markup extensions
            // `{loc:Translate ...}` se resuelven en ese momento: si el catálogo todavía no está
            // cargado, Loc.Get devuelve la clave cruda ("NavInicio") en lugar de la traducción.
            Localizacion.Inicializar();

            MainPage = new AppShell();
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
