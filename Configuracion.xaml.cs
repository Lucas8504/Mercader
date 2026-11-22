using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Mercader.Core.Services.Localization;
using Mercader.Services.Interfaces;
using Mercader.Services.Localization;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Mercader;

public partial class Configuracion : ContentPage
{
    private readonly INotificationService _notificationService;
    private readonly IDataRepository _repository;
    private readonly string[] _opcionesRecordatorio;
    private bool _ignorandoEventoIdioma;

    // El XAML declara SelectedIndex="0" y eso dispara SelectedIndexChanged durante
    // InitializeComponent, ANTES de leer la preferencia guardada: sin este guard el
    // handler pisaría la moneda elegida por el usuario con el primer item ("$").
    private bool _ignorandoEventoMoneda = true;

    public Configuracion(INotificationService notificationService, IDataRepository repository)
    {
        _notificationService = notificationService;
        _repository = repository;
        InitializeComponent();

        // Las opciones del recordatorio son texto de UI: se traducen según el idioma
        // activo. La lógica de notificaciones usa el índice, nunca el texto.
        _opcionesRecordatorio = new[]
        {
            Loc.Get("RecordatorioDiaEntrega"),
            Loc.Get("Recordatorio1HoraAntes"),
            Loc.Get("Recordatorio2HorasAntes"),
            Loc.Get("Recordatorio1DiaAntes"),
            Loc.Get("Recordatorio2DiasAntes")
        };
        TiempoRecordatorioPicker.ItemsSource = _opcionesRecordatorio;

        CargarConfiguracion();

        // La carga programática de la selección terminó: a partir de acá un cambio
        // del picker ya es del usuario y se guarda.
        _ignorandoEventoMoneda = false;
    }

    private void OnMonedaChanged(object sender, EventArgs e)
    {
        if (_ignorandoEventoMoneda)
            return;

        if (MonedaPicker.SelectedItem is not string moneda || string.IsNullOrWhiteSpace(moneda))
            return;

        Preferences.Set("moneda", moneda);
        Loc.SimboloMoneda = moneda;
    }

    private async void CargarConfiguracion()
    {
        var moneda = Preferences.Get("moneda", "$");

        for (int i = 0; i < MonedaPicker.Items.Count; i++)
        {
            if (MonedaPicker.Items[i] == moneda)
            {
                MonedaPicker.SelectedIndex = i;
                break;
            }
        }

        // Picker de idioma: items = nombre nativo, valor = código
        _ignorandoEventoIdioma = true;
        var nombres = new List<string>();
        var indiceActual = 0;
        for (int i = 0; i < Localizacion.Idiomas.Length; i++)
        {
            var (code, nombre) = Localizacion.Idiomas[i];
            nombres.Add(nombre);
            if (string.Equals(code, Loc.CurrentLanguage, StringComparison.OrdinalIgnoreCase))
                indiceActual = i;
        }
        IdiomaPicker.ItemsSource = nombres;
        IdiomaPicker.SelectedIndex = indiceActual;
        _ignorandoEventoIdioma = false;

        var notifEntregas = Preferences.Get("notifEntregas", false);
        NotificacionEntregasSwitch.IsToggled = notifEntregas;
        TiempoRecordatorioGrid.IsVisible = notifEntregas;

        var tiempo = Preferences.Get("tiempoRecordatorio", 1);
        if (tiempo >= 0 && tiempo < _opcionesRecordatorio.Length)
        {
            TiempoRecordatorioPicker.SelectedIndex = tiempo;
        }

        if (notifEntregas)
        {
            await ProgramarRecordatorios();
        }

        // Cargar tema guardado y resaltar el seleccionado
        var tema = Preferences.Get("tema", "system");
        ResaltarTema(tema);
    }

    /// <summary>
    /// Cambia el idioma de la app. Se recrea el Shell para que todo el XAML
    /// se vuelva a parsear con las nuevas traducciones.
    /// </summary>
    private void OnIdiomaChanged(object sender, EventArgs e)
    {
        if (_ignorandoEventoIdioma)
            return;

        var indice = IdiomaPicker.SelectedIndex;
        if (indice < 0 || indice >= Localizacion.Idiomas.Length)
            return;

        var code = Localizacion.Idiomas[indice].Code;
        Preferences.Set("idioma", code);

        var cambio = Loc.SetLanguage(code);
        if (!cambio)
            return;

        // Recrear el Shell para re-parsear el XAML con el nuevo idioma
        try
        {
            Application.Current.MainPage = new AppShell();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[IDIOMA] Error al recrear Shell: {ex.Message}");
        }
    }

    private void OnNotificacionEntregasToggled(object sender, ToggledEventArgs e)
    {
        Preferences.Set("notifEntregas", e.Value);
        TiempoRecordatorioGrid.IsVisible = e.Value;

        if (e.Value)
        {
            _ = ProgramarRecordatorios();
        }
    }

    private void OnTiempoRecordatorioChanged(object sender, EventArgs e)
    {
        Preferences.Set("tiempoRecordatorio", TiempoRecordatorioPicker.SelectedIndex);
        
        if (NotificacionEntregasSwitch.IsToggled)
        {
            _ = ProgramarRecordatorios();
        }
    }

    private async Task ProgramarRecordatorios()
    {
        try
        {
            await _notificationService.SolicitarPermisosAsync();

            var encargos = await _repository.GetEncargosAsync();
            var horas = ObtenerHoras();
            
            await _notificationService.ProgramarTodosLosRecordatoriosAsync(encargos, horas);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[NOTIF] Error: {ex.Message}");
        }
    }

    private int ObtenerHoras()
    {
        return TiempoRecordatorioPicker.SelectedIndex switch
        {
            0 => 0,
            1 => 1,
            2 => 2,
            3 => 24,
            4 => 48,
            _ => 1
        };
    }

    private void ResaltarTema(string tema)
    {
        var selected = Color.FromArgb("#0A84FF");
        var none = Colors.Transparent;

        TemaSistemaFrame.BorderColor = tema == "system" ? selected : none;
        TemaClaroFrame.BorderColor = tema == "light" ? selected : none;
        TemaOscuroFrame.BorderColor = tema == "dark" ? selected : none;
    }

    private void AplicarTema(string tema)
    {
        Preferences.Set("tema", tema);
        ResaltarTema(tema);
        App.AplicarTema();
    }

    private void OnTemaSistemaTapped(object sender, TappedEventArgs e) => AplicarTema("system");
    private void OnTemaClaroTapped(object sender, TappedEventArgs e) => AplicarTema("light");
    private void OnTemaOscuroTapped(object sender, TappedEventArgs e) => AplicarTema("dark");

    private async void OnExportarClicked(object sender, EventArgs e)
    {
        try
        {
            var accion = await DisplayActionSheet(Loc.Get("ExportarDatos"),
                Loc.Get("Cancelar"), null, Loc.Get("Ventas"), Loc.Get("Gastos"), Loc.Get("Encargos"));

            if (accion == null || accion == Loc.Get("Cancelar")) return;

            string contenido = "";
            string nombreArchivo = "";

            if (accion == Loc.Get("Ventas"))
            {
                var ventas = await _repository.GetVentasAsync();
                contenido = GenerarCsvVentas(ventas);
                nombreArchivo = $"ventas_{DateTime.Now:yyyyMMdd}.csv";
            }
            else if (accion == Loc.Get("Gastos"))
            {
                var gastos = await _repository.GetGastosAsync();
                contenido = GenerarCsvGastos(gastos);
                nombreArchivo = $"gastos_{DateTime.Now:yyyyMMdd}.csv";
            }
            else if (accion == Loc.Get("Encargos"))
            {
                var encargos = await _repository.GetEncargosAsync();
                contenido = GenerarCsvEncargos(encargos);
                nombreArchivo = $"encargos_{DateTime.Now:yyyyMMdd}.csv";
            }

            // Guardar archivo
            var ruta = Path.Combine(FileSystem.CacheDirectory, nombreArchivo);
            await File.WriteAllTextAsync(ruta, contenido);

            await DisplayAlert(Loc.Get("Exito"),
                Loc.Format("DatosExportados", nombreArchivo), "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert(Loc.Get("Error"), Loc.Format("ErrorExportar", ex.Message), "OK");
        }
    }

    private string GenerarCsvVentas(System.Collections.Generic.List<Domain.Entities.Ventas> ventas)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Fecha,Descripción,Precio,Cantidad,Total");

        foreach (var v in ventas)
        {
            sb.AppendLine($"{v.Fecha:yyyy-MM-dd},{v.Descripcion},{v.Precio},{v.Cantidad},{v.Precio * v.Cantidad}");
        }

        return sb.ToString();
    }

    private string GenerarCsvGastos(System.Collections.Generic.List<Domain.Entities.Gasto> gastos)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Fecha,Descripción,Monto,Cantidad,Total");

        foreach (var g in gastos)
        {
            sb.AppendLine($"{g.Fecha:yyyy-MM-dd},{g.Descripcion},{g.Monto},{g.Cantidad},{g.Monto * g.Cantidad}");
        }

        return sb.ToString();
    }

    private string GenerarCsvEncargos(System.Collections.Generic.List<Domain.Entities.Encargo> encargos)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Fecha,Cliente,Contacto,Descripción,Precio,Cantidad,Total,FechaEntrega");

        foreach (var en in encargos)
        {
            sb.AppendLine($"{en.Fecha:yyyy-MM-dd},{en.Nombre},{en.Contacto},{en.Descripcion},{en.Precio},{en.Cantidad},{en.Precio * en.Cantidad},{en.FechaEntrega:yyyy-MM-dd}");
        }

        return sb.ToString();
    }

    private async void OnImportarClicked(object sender, EventArgs e)
    {
        await DisplayAlert(Loc.Get("ImportarDatos"),
            Loc.Get("ImportarInstrucciones"), "OK");
    }

    private async void OnLimpiarDatosClicked(object sender, EventArgs e)
    {
        var confirmar = await DisplayAlert(Loc.Get("LimpiarDatosTitulo"),
            Loc.Get("ConfirmarLimpiar"),
            Loc.Get("SiEliminar"), Loc.Get("Cancelar"));

        if (confirmar)
        {
            var confirmar2 = await DisplayAlert(Loc.Get("LimpiarDatosTitulo"),
                Loc.Get("ConfirmarLimpiar2"),
                Loc.Get("SiEliminarTodo"), Loc.Get("Cancelar"));

            if (confirmar2)
            {
                try
                {
                    // Obtener todos los datos
                    var ventas = await _repository.GetVentasAsync();
                    var gastos = await _repository.GetGastosAsync();
                    var encargos = await _repository.GetEncargosAsync();

                    // Eliminar uno por uno (soft delete)
                    foreach (var v in ventas)
                    {
                        await _repository.DeleteVentaAsync(v);
                    }
                    foreach (var g in gastos)
                    {
                        await _repository.DeleteGastoAsync(g);
                    }
                    foreach (var en in encargos)
                    {
                        await _repository.DeleteEncargoAsync(en);
                    }

                    await DisplayAlert(Loc.Get("Exito"),
                        Loc.Format("DatosEliminados", ventas.Count, gastos.Count, encargos.Count), "OK");
                }
                catch (Exception ex)
                {
                    await DisplayAlert(Loc.Get("Error"), Loc.Format("ErrorLimpiar", ex.Message), "OK");
                }
            }
        }
    }
}
