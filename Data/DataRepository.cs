using SQLite;
using Mercader.Domain.Entities;
using Mercader.Data.Interfaces;

namespace Mercader.Data
{
    /// <summary>
    /// Implementación de IDataRepository usando SQLite.
    /// </summary>
    public sealed class DataRepository : IDataRepository
    {
        private SQLiteAsyncConnection? _database;
        private readonly string _dbPath;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public DataRepository()
        {
            _dbPath = Path.Combine(
                FileSystem.AppDataDirectory,
                "MercaderDB.db3"
            );
        }

        public async Task InitializeDatabaseAsync()
        {
            try
            {
                await _semaphore.WaitAsync();

                if (_database is not null)
                    return;

                _database = new SQLiteAsyncConnection(_dbPath);
                await _database.CreateTableAsync<Encargo>();
                await _database.CreateTableAsync<Ventas>();
                await _database.CreateTableAsync<Gasto>();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        // ===== ENCARGOS =====

        public async Task<List<Encargo>> GetEncargosAsync()
        {
            if (_database is null)
                throw new InvalidOperationException("La base de datos no está inicializada.");

            return await _database.Table<Encargo>()
                .Where(x => x.IsDeleted != true)
                .ToListAsync();
        }

        public async Task<int> SaveEncargoAsync(Encargo encargo)
        {
            ArgumentNullException.ThrowIfNull(encargo);

            if (_database is null)
                throw new InvalidOperationException("La base de datos no está inicializada.");

            // Set timestamps
            if (encargo.Id == 0)
                encargo.CreatedAt = DateTime.UtcNow;
            else
                encargo.UpdatedAt = DateTime.UtcNow;

            return encargo.Id != 0
                ? await _database.UpdateAsync(encargo)
                : await _database.InsertAsync(encargo);
        }

        public async Task<int> DeleteEncargoAsync(Encargo encargo)
        {
            ArgumentNullException.ThrowIfNull(encargo);

            if (_database is null)
                throw new InvalidOperationException("La base de datos no está inicializada.");

            // Soft delete
            encargo.SoftDelete();
            return await _database.UpdateAsync(encargo);
        }

        // ===== GASTOS =====

        public async Task<List<Gasto>> GetGastosAsync()
        {
            if (_database is null)
                throw new InvalidOperationException("La base de datos no está inicializada.");

            return await _database.Table<Gasto>()
                .Where(x => x.IsDeleted != true)
                .ToListAsync();
        }

        public async Task<int> SaveGastoAsync(Gasto gasto)
        {
            ArgumentNullException.ThrowIfNull(gasto);

            if (_database is null)
                throw new InvalidOperationException("La base de datos no está inicializada.");

            // Set timestamps
            if (gasto.Id == 0)
                gasto.CreatedAt = DateTime.UtcNow;
            else
                gasto.UpdatedAt = DateTime.UtcNow;

            return gasto.Id != 0
                ? await _database.UpdateAsync(gasto)
                : await _database.InsertAsync(gasto);
        }

        public async Task<int> DeleteGastoAsync(Gasto gasto)
        {
            ArgumentNullException.ThrowIfNull(gasto);

            if (_database is null)
                throw new InvalidOperationException("La base de datos no está inicializada.");

            // Soft delete
            gasto.SoftDelete();
            return await _database.UpdateAsync(gasto);
        }

        // ===== VENTAS =====

        public async Task<List<Ventas>> GetVentasAsync()
        {
            if (_database is null)
                throw new InvalidOperationException("La base de datos no está inicializada.");

            var all = await _database.Table<Ventas>().ToListAsync();
            // Filtrar solo los no eliminados - maneja null como false
            var filtered = all.Where(x => x.IsDeleted != true).ToList();
            
            // Debug
            System.Diagnostics.Debug.WriteLine($"[DEBUG] GetVentasAsync: {all.Count} total, {filtered.Count} sin eliminar");
            foreach(var v in all)
            {
                System.Diagnostics.Debug.WriteLine($"  - Id:{v.Id}, IsDeleted:{v.IsDeleted}, Desc:{v.Descripcion}");
            }
            
            return filtered;
        }

        public async Task<int> SaveVentasAsync(Ventas venta)
        {
            ArgumentNullException.ThrowIfNull(venta);

            await _semaphore.WaitAsync();
            try
            {
                if (_database is null)
                    throw new InvalidOperationException("Base de datos no inicializada");

                // Set timestamps
                if (venta.Id == 0)
                    venta.CreatedAt = DateTime.UtcNow;
                else
                    venta.UpdatedAt = DateTime.UtcNow;

                return venta.Id != 0
                    ? await _database.UpdateAsync(venta)
                    : await _database.InsertAsync(venta);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<int> DeleteVentaAsync(Ventas venta)
        {
            ArgumentNullException.ThrowIfNull(venta);

            if (_database is null)
                throw new InvalidOperationException("La base de datos no está inicializada.");

            // Debug
            System.Diagnostics.Debug.WriteLine($"[DEBUG] DeleteVentaAsync: Recibido Id={venta.Id}, IsDeleted={venta.IsDeleted}");
            
            // Soft delete
            venta.SoftDelete();
            System.Diagnostics.Debug.WriteLine($"[DEBUG] DeleteVentaAsync: Después softdelete IsDeleted={venta.IsDeleted}");
            
            var result = await _database.UpdateAsync(venta);
            System.Diagnostics.Debug.WriteLine($"[DEBUG] DeleteVentaAsync: UpdateAsync result={result}");
            
            return result;
        }

        // ===== CONSULTAS ESPECÍFICAS =====

        public async Task<List<Ventas>> GetVentasUltimos6MesesAsync()
        {
            if (_database is null)
                throw new InvalidOperationException("BD no inicializada");

            DateTime fechaInicio = DateTime.Now.AddMonths(-6).Date;
            return await _database.Table<Ventas>()
                .Where(v => v.Fecha >= fechaInicio && v.IsDeleted != true)
                .OrderBy(v => v.Fecha)
                .ToListAsync();
        }

        public async Task<List<Gasto>> GetGastosUltimos6MesesAsync()
        {
            if (_database is null)
                throw new InvalidOperationException("La base de datos no está inicializada.");

            DateTime fechaInicio = DateTime.Now.AddMonths(-6);
            return await _database.Table<Gasto>()
                .Where(g => g.Fecha >= fechaInicio && g.IsDeleted != true)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene TODOS los registros incluyendo los eliminados (para auditoría).
        /// </summary>
        public async Task<List<T>> GetAllAsync<T>() where T : BaseEntity, new()
        {
            if (_database is null)
                throw new InvalidOperationException("BD no inicializada");

            return await _database.Table<T>().ToListAsync();
        }

        /// <summary>
        /// Obtiene solo los eliminados (soft-delete).
        /// </summary>
        public async Task<List<T>> GetDeletedAsync<T>() where T : BaseEntity, new()
        {
            if (_database is null)
                throw new InvalidOperationException("BD no inicializada");

            return await _database.Table<T>()
                .Where(x => x.IsDeleted)
                .ToListAsync();
        }

        // ===== DISPOSABLE =====

        public void Dispose()
        {
            _semaphore.Dispose();
            _database?.CloseAsync().Wait();
        }
    }
}
