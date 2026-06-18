using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Mercader.Services.Interfaces;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Mercader;

public partial class Configuracion : ContentPage
{
    private readonly INotificationService _notificationService;
    private readonly IDataRepository _repository;

    public Configuracion(INotificationService notificationService, IDataRepository repository)
    {
        _notificationService = notificationService;
        _repository = repository;
        InitializeComponent();
        CargarConfiguracion();
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

        var notifEntregas = Preferences.Get("notifEntregas", false);
        NotificacionEntregasSwitch.IsToggled = notifEntregas;
        TiempoRecordatorioGrid.IsVisible = notifEntregas;

        var tiempo = Preferences.Get("tiempoRecordatorio", 1);
        if (tiempo >= 0 && tiempo < TiempoRecordatorioPicker.Items.Count)
        {
            TiempoRecordatorioPicker.SelectedIndex = tiempo;
        }

        if (notifEntregas)
        {
            await ProgramarRecordatorios();
        }

        // Cargar tema guardado
        var tema = Preferences.Get("tema", "system");
        TemaPicker.SelectedIndex = tema switch
        {
            "light" => 1,
            "dark" => 2,
            _ => 0
        };
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

    private void OnTemaChanged(object sender, EventArgs e)
    {
        var tema = TemaPicker.SelectedIndex switch
        {
            1 => "light",
            2 => "dark",
            _ => "system"
        };

        Preferences.Set("tema", tema);
        App.AplicarTema();
    }

    private async void OnExportarClicked(object sender, EventArgs e)
    {
        try
        {
            var accion = await DisplayActionSheet("📤 Exportar Datos",
                "Cancelar", null, "Ventas", "Gastos", "Encargos");

            if (accion == null || accion == "Cancelar") return;

            string contenido = "";
            string nombreArchivo = "";

            if (accion == "Ventas")
            {
                var ventas = await _repository.GetVentasAsync();
                contenido = GenerarCsvVentas(ventas);
                nombreArchivo = $"ventas_{DateTime.Now:yyyyMMdd}.csv";
            }
            else if (accion == "Gastos")
            {
                var gastos = await _repository.GetGastosAsync();
                contenido = GenerarCsvGastos(gastos);
                nombreArchivo = $"gastos_{DateTime.Now:yyyyMMdd}.csv";
            }
            else if (accion == "Encargos")
            {
                var encargos = await _repository.GetEncargosAsync();
                contenido = GenerarCsvEncargos(encargos);
                nombreArchivo = $"encargos_{DateTime.Now:yyyyMMdd}.csv";
            }

            // Guardar archivo
            var ruta = Path.Combine(FileSystem.CacheDirectory, nombreArchivo);
            await File.WriteAllTextAsync(ruta, contenido);

            await DisplayAlert("✅ Éxito",
                $"Datos exportados a:\n{nombreArchivo}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("❌ Error", $"Error al exportar: {ex.Message}", "OK");
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
        await DisplayAlert("📥 Importar",
            "Para importar datos:\n\n1. Copiá el archivo CSV a la carpeta de la app\n2. Los datos se agregarán a los existentes\n\nFormatos soportados: CSV con headers", "OK");
    }

    private async void OnLimpiarDatosClicked(object sender, EventArgs e)
    {
        var confirmar = await DisplayAlert("🗑️ Limpiar Datos",
            "¿Estás seguro de que querés eliminar TODOS los datos?\n\nEsta acción no se puede deshacer.",
            "Sí, eliminar", "Cancelar");

        if (confirmar)
        {
            var confirmar2 = await DisplayAlert("⚠️ Confirmar",
                "Esto eliminará:\n- Todas las ventas\n- Todos los gastos\n- Todos los encargos\n\n¿Continuar?",
                "Sí, eliminar todo", "Cancelar");

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

                    await DisplayAlert("✅ Éxito",
                        $"Datos eliminados:\n- {ventas.Count} ventas\n- {gastos.Count} gastos\n- {encargos.Count} encargos", "OK");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("❌ Error", $"Error al limpiar: {ex.Message}", "OK");
                }
            }
        }
    }
}