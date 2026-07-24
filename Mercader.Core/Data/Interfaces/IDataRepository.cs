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

        // ===== AUTOCOMPLETADO =====
        Task<List<string>> GetDistinctVentasDescriptionsAsync();
        Task<List<string>> GetDistinctGastosDescriptionsAsync();
        Task<List<string>> GetDistinctEncargosDescriptionsAsync();
        Task<List<string>> GetDistinctEncargosNombresAsync();
        Task DismissAutocompleteDescriptionAsync(string descripcion, string entityType);
        Task ReinstateAutocompleteDescriptionAsync(string descripcion, string entityType);

        // ===== ARTÍCULOS DE VENTA =====
        Task<List<ArticuloVenta>> GetArticulosVentaAsync(int ventaId);
        Task<int> SaveArticuloVentaAsync(ArticuloVenta articulo);
        Task<int> DeleteArticuloVentaAsync(ArticuloVenta articulo);

        // ===== ARTÍCULOS DE GASTO =====
        Task<List<ArticuloGasto>> GetArticulosGastoAsync(int gastoId);
        Task<int> SaveArticuloGastoAsync(ArticuloGasto articulo);
        Task<int> DeleteArticuloGastoAsync(ArticuloGasto articulo);

        // ===== ARTÍCULOS DE ENCARGO =====
        Task<List<ArticuloEncargo>> GetArticulosEncargoAsync(int encargoId);
        Task<int> SaveArticuloEncargoAsync(ArticuloEncargo articulo);
        Task<int> DeleteArticuloEncargoAsync(ArticuloEncargo articulo);

        // ===== MÉTODOS GENÉRICOS (AUDITORÍA) =====
        Task<List<T>> GetAllAsync<T>() where T : BaseEntity, new();
        Task<List<T>> GetDeletedAsync<T>() where T : BaseEntity, new();
    }
}
