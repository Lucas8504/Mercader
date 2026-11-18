using CommunityToolkit.Maui.Storage;
using Mercader.Services.Interfaces;
using Mercader.ViewModels;
using Microcharts;
using SkiaSharp;

namespace Mercader
{
    public partial class MainPage : ContentPage
    {
        private readonly MainViewModel _viewModel;
        private readonly IExportPdfService _pdfService;
        private readonly IBalanceCalculatorService _balanceCalculator;
        private readonly IChartService _chartService;

        // ==========================================
        // LOCALIZATION SETUP (TODO: instalar Maui.Localization)
        // ==========================================
        // Pasos necesarios:
        // 1. dotnet add package Maui.Localization (o verificar que está en el proyecto)
        // 2. Agregar en App.xaml.cs:
        //    using Maui.Localization;
        //    builder.UseMauiLocalizer(options => options.SetDefaultCulture(CultureInfo.GetCultureInfo("es")));
        // 3. Crear archivos de recursos:
        //    - Resources.resx (español - por defecto)
        //    - Resources_en.resx (inglés - ya creado)
        //    - Resources_pt_BR.resx (portugués)
        //    - Resources_zh_TW.resx (chino)
        //    - Resources_ru.resx (ruso)
        // 4. Registrar recursos en el constructor:
        //    var localizationManager = new LocalizationManager(this);
        //    localizationManager.RegisterResource("Total", "Total", Resources.resx);
        //    localizationManager.RegisterResource("Total", "Total", Resources_en.resx);
        //    ... repite para cada idioma ...
        // 5. Binding en XAML:
        //    <Label Text="{ResX.Id('Total')}" />
        // ==========================================

        public MainPage(
            MainViewModel vm,
            IExportPdfService pdfService,
            IBalanceCalculatorService balanceCalculator,
            IChartService chartService)
        {
            InitializeComponent();
            _viewModel = vm;
            _pdfService = pdfService;
            _balanceCalculator = balanceCalculator;
            _chartService = chartService;
            BindingContext = _viewModel;
        }
        
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                // Usar el Command del ViewModel
                if (_viewModel.CargarDatosCommand.CanExecute(null))
                {
                    await _viewModel.CargarDatosCommand.ExecuteAsync(null);
                }

                // Scroll a los gráficos después de cargar
                await Task.WhenAll(
                    ScrollToEndAsync(VentasScroll),
                    ScrollToEndAsync(GastosScroll),
                    ScrollToEndAsync(EncargosScroll),
                    ScrollToEndAsync(GananciasScroll)
                );
            }
            catch (Exception ex)
            {

                System.Diagnostics.Debug.WriteLine($"[ERROR] MainPage.OnAppearing: {ex}");
            }
        }
        }