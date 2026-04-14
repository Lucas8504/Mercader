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

            // Inicializar la base de datos una sola vez
            Task.Run(async () =>
            {
                await repo.InitializeDatabaseAsync();
            });

            MainPage = shell;
        }
    }
}
