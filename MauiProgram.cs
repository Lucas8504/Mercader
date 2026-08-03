using CommunityToolkit.Maui;
using Syncfusion.Maui.Toolkit.Hosting;
using Mercader.ViewModels;
using Mercader.Services.Interfaces;
using Mercader.Services;
using Mercader.Data.Interfaces;
using Mercader.Services.Calculators;
using Mercader.Services.Charts;
using Microcharts.Maui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
#if ANDROID
using Android.Content.Res;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
#endif

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
                .ConfigureSyncfusionToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if ANDROID
            // Eliminar underline de Entry en Android
            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoUnderline", (handler, entry) =>
            {
                if (handler.PlatformView is Android.Widget.EditText editText)
                {
                    editText.BackgroundTintList = ColorStateList.ValueOf(Colors.Transparent.ToPlatform());
                }
            });
#endif
            
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
            builder.Services.AddSingleton<IExportPdfService, ExportPdfService>();

            // Shell
            builder.Services.AddSingleton<AppShell>();

            // Pages
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<Gastos>();
            builder.Services.AddTransient<Encargos>();
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
