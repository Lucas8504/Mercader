using Mercader.Domain.Entities;

namespace Mercader.Data.Interfaces
{
    /// <summary>
    /// Repositorio de datos para la base de datos SQLite.
    /// </summary>
    public interface IDataRepository
    {
        /// <summary>
        /// Inicializa la base de datos y crea las tablas si no existen.
        /// </summary>
        Task InitializeDatabaseAsync();

        // ===== ENCARGOS =====
        Task<List<Encargo>> GetEncargosAsync();
        Task<int> SaveEncargoAsync(Encargo encargo);
        Task<int> DeleteEncargoAsync(Encargo encargo);

        // ===== GASTOS =====
        Task<List<Gasto>> GetGastosAsync();
        Task<int> SaveGastoAsync(Gasto gasto);
        Task<int> DeleteGastoAsync(Gasto gasto);

        // ===== VENTAS =====
        Task<List<Ventas>> GetVentasAsync();
        Task<int> SaveVentasAsync(Ventas venta);
        Task<int> DeleteVentaAsync(Ventas venta);

        // ===== CONSULTAS ESPECÍFICAS =====
        Task<List<Ventas>> GetVentasUltimos6MesesAsync();
        Task<List<Gasto>> GetGastosUltimos6MesesAsync();
    }
}
