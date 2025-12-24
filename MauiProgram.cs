using Microcharts.Maui;
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
            
            builder.Services.AddSingleton<AppShell>();

            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<Gastos>();
            builder.Services.AddTransient<Encargos>();
            builder.Services.AddTransient<DetalleEncargo>();
            builder.Services.AddTransient<EditarEncargoPage>();
            builder.Services.AddTransient<Venta>();

            builder.Services.AddSingleton<DataRepository>();


#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
