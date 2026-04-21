using Mercader.Domain.Entities;

namespace Mercader.Services.Interfaces
{
    public interface INotificationService
    {
        Task SolicitarPermisosAsync();
        Task ProgramarRecordatorioEntregaAsync(Encargo encargo, int horasAntes);
        Task CancelarRecordatorio(int encargoId);
        Task ProgramarTodosLosRecordatoriosAsync(IEnumerable<Encargo> encargos, int horasAntes);
    }
}