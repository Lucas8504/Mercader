using System.Diagnostics;

namespace Mercader
{
    public partial class App : Application
    {
        public App(AppShell shell, DataRepository repo)
        {
            InitializeComponent();

            SQLitePCL.Batteries_V2.Init();

            // Inicializar la base de datos una sola vez
            Task.Run(async () =>
            {
                await repo.InitializeDatabaseAsync();
            });

            MainPage = shell;
        }
    }
}
