using SQLite;
using Mercader.Domain.Entities;
using Mercader.Data.Interfaces;

namespace Mercader.Data
{
    /// <summary>
    /// Implementación de IDataRepository usando SQLite.
    /// </summary>
    public sealed class DataRepository : IDataRepository, IAsyncDisposable
    {
        private SQLiteAsyncConnection? _database;
        private readonly string _dbPath;
        public DataRepository()
        {
            _dbPath = Path.Combine(
                FileSystem.AppDataDirectory,
                "MercaderDB.db3"
            );
        }

        public async Task InitializeDatabaseAsync()
        {
            if (_database is not null)
                return;

            _database = new SQLiteAsyncConnection(_dbPath);
            
            // Crear tablas
            await _database.CreateTableAsync<Encargo>();
            await _database.CreateTableAsync<Ventas>();
            await _database.CreateTableAsync<Gasto>();
            await _database.CreateTableAsync<DescripcionOculta>();
            
            // Ejecutar migraciones
            await RunMigrationsAsync();
        }

        /// <summary>
        /// Ejecuta migraciones para agregar columnas faltantes.
        /// </summary>
        private async Task RunMigrationsAsync()
        {
            if (_database is null)
                return;

            try
            {
                // Migración: Agregar columnas de auditoría a Ventas
                await AddColumnIfNotExistsAsync("Ventas", "CreatedAt", "TEXT");
                await AddColumnIfNotExistsAsync("Ventas", "UpdatedAt", "TEXT");
                await AddColumnIfNotExistsAsync("Ventas", "IsDeleted", "INTEGER DEFAULT 0");

                // Migración: Agregar columnas de auditoría a Gastos
                await AddColumnIfNotExistsAsync("Gastos", "CreatedAt", "TEXT");
                await AddColumnIfNotExistsAsync("Gastos", "UpdatedAt", "TEXT");
                await AddColumnIfNotExistsAsync("Gastos", "IsDeleted", "INTEGER DEFAULT 0");

                // Migración: Agregar columnas de auditoría a Encargo
                await AddColumnIfNotExistsAsync("Encargo", "CreatedAt", "TEXT");
                await AddColumnIfNotExistsAsync("Encargo", "UpdatedAt", "TEXT");
                await AddColumnIfNotExistsAsync("Encargo", "IsDeleted", "INTEGER DEFAULT 0");

                System.Diagnostics.Debug.WriteLine("[MIGRATION] Migraciones ejecutadas exitosamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MIGRATION] Error en migraciones: {ex.Message}");
            }
        }

        /// <summary>
        /// Agrega una columna a una tabla si no existe.
        /// Intenta siempre y captura error si ya existe.
        /// </summary>
        private async Task AddColumnIfNotExistsAsync(string tableName, string columnName, string columnDefinition)
        {
            if (_database is null)
                return;

            try
            {
                // Intentar agregar la columna directamente
                // SQLite tirará error si ya existe, lo cual es OK
                await _database.ExecuteAsync(
                    $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition}");
                
                System.Diagnostics.Debug.WriteLine($"[MIGRATION] Columna {columnName} agregada a {tableName}");
            }
            catch (Exception ex)
            {
                // Si el error dice que la columna ya existe, está OK
                // Cualquier otro error, lo registramos
                if (!ex.Message.Contains("duplicate column name"))
                {
                    System.Diagnostics.Debug.WriteLine($"[MIGRATION] {tableName}.{columnName}: {ex.Message}");
                }
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

            return await _database.Table<Ventas>()
                .Where(x => x.IsDeleted != true)
                .ToListAsync();
        }

        public async Task<int> SaveVentasAsync(Ventas venta)
        {
            ArgumentNullException.ThrowIfNull(venta);

            if (_database is null)
                throw new InvalidOperationException("La base de datos no está inicializada.");

            // Set timestamps
            if (venta.Id == 0)
                venta.CreatedAt = DateTime.UtcNow;
            else
                venta.UpdatedAt = DateTime.UtcNow;

            return venta.Id != 0
                ? await _database.UpdateAsync(venta)
                : await _database.InsertAsync(venta);
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

        // ===== AUTOCOMPLETADO =====

        public async Task<List<string>> GetDistinctVentasDescriptionsAsync()
        {
            if (_database is null)
                throw new InvalidOperationException("La base de datos no está inicializada.");

            var descriptions = await _database.QueryAsync<Ventas>(
                @"SELECT DISTINCT v.Descripcion
                  FROM Ventas v
                  LEFT JOIN DescripcionOculta d ON v.Descripcion = d.Descripcion
                  WHERE v.Descripcion IS NOT NULL
                    AND v.Descripcion != ''
                    AND v.IsDeleted != 1
                    AND d.Id IS NULL
                  ORDER BY v.Descripcion");

            return descriptions
                .Where(v => v.Descripcion is not null)
                .Select(v => v.Descripcion!)
                .ToList();
        }

        public async Task<List<string>> GetDistinctGastosDescriptionsAsync()
        {
            if (_database is null)
                throw new InvalidOperationException("La base de datos no está inicializada.");

            var descriptions = await _database.QueryAsync<Gasto>(
                @"SELECT DISTINCT g.Descripcion
                  FROM Gastos g
                  LEFT JOIN DescripcionOculta d ON g.Descripcion = d.Descripcion
                  WHERE g.Descripcion IS NOT NULL
                    AND g.Descripcion != ''
                    AND g.IsDeleted != 1
                    AND d.Id IS NULL
                  ORDER BY g.Descripcion");

            return descriptions
                .Where(g => g.Descripcion is not null)
                .Select(g => g.Descripcion!)
                .ToList();
        }

        public async Task<List<string>> GetDistinctEncargosDescriptionsAsync()
        {
            if (_database is null)
                throw new InvalidOperationException("La base de datos no está inicializada.");

            var descriptions = await _database.QueryAsync<Encargo>(
                @"SELECT DISTINCT e.Descripcion
                  FROM Encargo e
                  LEFT JOIN DescripcionOculta d ON e.Descripcion = d.Descripcion
                  WHERE e.Descripcion IS NOT NULL
                    AND e.Descripcion != ''
                    AND e.IsDeleted != 1
                    AND d.Id IS NULL
                  ORDER BY e.Descripcion");

            return descriptions
                .Where(e => e.Descripcion is not null)
                .Select(e => e.Descripcion!)
                .ToList();
        }

        public async Task<List<string>> GetDistinctEncargosNombresAsync()
        {
            if (_database is null)
                throw new InvalidOperationException("La base de datos no está inicializada.");

            var nombres = await _database.QueryAsync<Encargo>(
                @"SELECT DISTINCT e.Nombre
                  FROM Encargo e
                  LEFT JOIN DescripcionOculta d ON e.Nombre = d.Descripcion
                  WHERE e.Nombre IS NOT NULL
                    AND e.Nombre != ''
                    AND e.IsDeleted != 1
                    AND d.Id IS NULL
                  ORDER BY e.Nombre");

            return nombres
                .Where(e => e.Nombre is not null)
                .Select(e => e.Nombre!)
                .ToList();
        }

        public async Task DismissAutocompleteDescriptionAsync(string descripcion)
        {
            if (_database is null)
                throw new InvalidOperationException("La base de datos no está inicializada.");

            if (string.IsNullOrWhiteSpace(descripcion))
                return;

            // Solo inserta si no existe ya
            var existe = await _database.FindAsync<DescripcionOculta>(d => d.Descripcion == descripcion);
            if (existe is null)
            {
                await _database.InsertAsync(new DescripcionOculta
                {
                    Descripcion = descripcion
                });
            }
        }

        public async Task ReinstateAutocompleteDescriptionAsync(string descripcion)
        {
            if (_database is null)
                throw new InvalidOperationException("La base de datos no está inicializada.");

            if (string.IsNullOrWhiteSpace(descripcion))
                return;

            var existente = await _database.Table<DescripcionOculta>()
                .FirstOrDefaultAsync(d => d.Descripcion == descripcion);
            if (existente is not null)
                await _database.DeleteAsync(existente);
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

        public async ValueTask DisposeAsync()
        {
            if (_database != null)
                await _database.CloseAsync();
            GC.SuppressFinalize(this);
        }
    }
}
