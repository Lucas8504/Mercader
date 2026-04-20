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
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });
            
            // Data
            builder.Services.AddSingleton<Data.DataRepository>();
            builder.Services.AddSingleton<IDataRepository>(sp => sp.GetRequiredService<Data.DataRepository>());

            // Services
            builder.Services.AddSingleton<IChartService, ChartService>();
            builder.Services.AddSingleton<IBalanceCalculatorService, BalanceCalculatorService>();

            // Shell
            builder.Services.AddSingleton<AppShell>();

            // Pages
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<Gastos>();
            builder.Services.AddTransient<Encargos>();
            builder.Services.AddTransient<DetalleEncargo>();
            builder.Services.AddTransient<EditarEncargoPage>();
            builder.Services.AddTransient<Venta>();

            // ViewModels
            builder.Services.AddTransient<MainViewModel>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
