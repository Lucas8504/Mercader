using CommunityToolkit.Maui;
using Mercader.ViewModels;
using Mercader.Services.Interfaces;
using Mercader.Data.Interfaces;
using Mercader.Services.Calculators;
using Mercader.Services.Charts;
using Microcharts.Maui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mercader
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMicrocharts()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });
            
            // Data
            builder.Services.AddSingleton<Data.DataRepository>(sp =>
                new Data.DataRepository(Path.Combine(FileSystem.AppDataDirectory, "MercaderDB.db3")));
            builder.Services.AddSingleton<IDataRepository>(sp => sp.GetRequiredService<Data.DataRepository>());

            // ViewModels
            builder.Services.AddTransient<VentasViewModel>();
            builder.Services.AddTransient<GastosViewModel>();
            builder.Services.AddTransient<EncargosViewModel>();

            // Services
            builder.Services.AddSingleton<IChartService, ChartService>();
            builder.Services.AddSingleton<IBalanceCalculatorService, BalanceCalculatorService>();
            builder.Services.AddSingleton<INotificationService, NotificationService>();
            builder.Services.AddSingleton<INavigationService, NavigationService>();

            // Shell
            builder.Services.AddSingleton<AppShell>();

            // Pages
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<Gastos>();
            builder.Services.AddTransient<Encargos>();
            builder.Services.AddTransient<DetalleEncargo>();
            builder.Services.AddTransient<EditarEncargoPage>();
            builder.Services.AddTransient<Venta>();
            builder.Services.AddTransient<Configuracion>();

            // Modals
            builder.Services.AddTransient<VentaModal>();
            builder.Services.AddTransient<GastoModal>();
            builder.Services.AddTransient<EncModal>();

            // ViewModels
            builder.Services.AddTransient<MainViewModel>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
