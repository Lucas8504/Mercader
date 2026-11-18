using CommunityToolkit.Maui.Storage;
using Mercader.Services.Interfaces;
using Mercader.ViewModels;
using Microcharts;
using SkiaSharp;
using Maui.Localization;

namespace Mercader
{
    public partial class MainPage : ContentPage
    {
        private readonly MainViewModel _viewModel;
        private readonly IExportPdfService _pdfService;
        private readonly IBalanceCalculatorService _balanceCalculator;
        private readonly IChartService _chartService;
        private readonly LocalizationManager _localizationManager;

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
            
            // Localización por idioma (en-US como piloto)
            _localizationManager = new LocalizationManager(this);
            
            // Registrar recursos por idioma
            _localizationManager.RegisterResource("Total", "Total", Resources_en.resx);
            _localizationManager.RegisterResource("Período", "Period", Resources_en.resx);
            _localizationManager.RegisterResource("Ventas", "Sales", Resources_en.resx);
            _localizationManager.RegisterResource("Gastos", "Expenses", Resources_en.resx);
            _localizationManager.RegisterResource("Ganancias", "Profit", Resources_en.resx);
            _localizationManager.RegisterResource("Margen", "Margin", Resources_en.resx);
            _localizationManager.RegisterResource("Resumen", "Summary", Resources_en.resx);
            _localizationManager.RegisterResource("Encargos", "Encargos", Resources_en.resx);
            _localizationManager.RegisterResource("Articulos", "Articles", Resources_en.resx);
            _localizationManager.RegisterResource("Agregar", "Add", Resources_en.resx);
            _localizationManager.RegisterResource("Cancelar", "Cancel", Resources_en.resx);
            _localizationManager.RegisterResource("Error", "Error", Resources_en.resx);
            _localizationManager.RegisterResource("Descripcion", "Description", Resources_en.resx);
            _localizationManager.RegisterResource("Precio", "Price", Resources_en.resx);
            _localizationManager.RegisterResource("Cantidad", "Quantity", Resources_en.resx);
            _localizationManager.RegisterResource("Imagen", "Image", Resources_en.resx);
            _localizationManager.RegisterResource("Camera", "Camera", Resources_en.resx);
            _localizationManager.RegisterResource("+ Agregar artículo", "Add Article", Resources_en.resx);
            _localizationManager.RegisterResource("Agregar Venta", "Add Sale", Resources_en.resx);
            _localizationManager.RegisterResource("Gasto", "Expense", Resources_en.resx);
            _localizationManager.RegisterResource("Venta", "Sale", Resources_en.resx);
            _localizationManager.RegisterResource("Articulo", "Article", Resources_en.resx);
            _localizationManager.RegisterResource("Imágenes", "Images", Resources_en.resx);
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