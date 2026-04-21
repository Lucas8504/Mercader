using Mercader.Domain.Entities;
using Mercader.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mercader.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IDataRepository _repository;

        public NotificationService(IDataRepository repository)
        {
            _repository = repository;
        }

        public async Task SolicitarPermisosAsync()
        {
            // Los permisos se solicitarian aqui en una implementacion real con platform-specific
            // Para efectos.demo, simplemente cargamos los encargos
            System.Diagnostics.Debug.WriteLine("[NOTIF] Solicitando permisos de notificacion...");
            await Task.CompletedTask;
        }

        public async Task ProgramarRecordatorioEntregaAsync(Encargo encargo, int horasAntes)
        {
            // Calcular fecha de notificacion
            var fechaNotificacion = encargo.FechaEntrega.AddHours(-horasAntes);

            if (fechaNotificacion <= DateTime.Now)
            {
                return;
            }

            var titulo = horasAntes == 0 
                ? "📦 ¡Entrega HOY!" 
                : $"⏰ Entrega en {horasAntes} hora" + (horasAntes > 1 ? "s" : "");

            var mensaje = $"{encargo.Nombre}: {encargo.Descripcion}";

            // En una implementacion real, aqui se llamaria a NotificationCenter.Default.Show()
            System.Diagnostics.Debug.WriteLine($"[NOTIF] {titulo} - {mensaje} (programado: {fechaNotificacion})");
            
            await Task.CompletedTask;
        }

        public Task CancelarRecordatorio(int encargoId)
        {
            System.Diagnostics.Debug.WriteLine($"[NOTIF] Cancelando recordatorio {encargoId}");
            return Task.CompletedTask;
        }

        public async Task ProgramarTodosLosRecordatoriosAsync(IEnumerable<Encargo> encargos, int horasAntes)
        {
            int count = 0;
            foreach (var encargo in encargos)
            {
                if (encargo.FechaEntrega > DateTime.Now)
                {
                    await ProgramarRecordatorioEntregaAsync(encargo, horasAntes);
                    count++;
                }
            }
            System.Diagnostics.Debug.WriteLine($"[NOTIF] {count} recordatorios programados ({horasAntes}h antes)");
        }
    }
}