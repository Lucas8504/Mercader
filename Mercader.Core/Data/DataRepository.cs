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

        /// <summary>
        /// Crea el repositorio con la ruta completa al archivo de base de datos.
        /// </summary>
        public DataRepository(string dbPath)
        {
            _dbPath = dbPath;
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
            await _database.CreateTableAsync<ArticuloVenta>();
            await _database.CreateTableAsync<ArticuloGasto>();
            await _database.CreateTableAsync<ArticuloEncargo>();

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

                // Migración: EntityType a DescripcionOculta para scoping por entidad
                await AddColumnIfNotExistsAsync("DescripcionOculta", "EntityType", "TEXT");

                // Migración: Migrar datos legacy a tablas de artículos
                await MigrateLegacyVentasAsync();
                await MigrateLegacyGastosAsync();
                await MigrateLegacyEncargosAsync();

                // Migración: Agregar columna ImagenPath a ArticulosVenta
                await AddColumnIfNotExistsAsync("ArticulosVenta", "ImagenPath", "TEXT");
                // Migración: Agregar columna ImagenPath a ArticulosGasto
                await AddColumnIfNotExistsAsync("ArticulosGasto", "ImagenPath", "TEXT");
                // Migración: Agregar columna ImagenPath a ArticulosEncargo
                await AddColumnIfNotExistsAsync("ArticulosEncargo", "ImagenPath", "TEXT");

                // Migración: Agregar columnas ImagenPath1-4 para 4 imágenes por artículo
                await AddColumnIfNotExistsAsync("ArticulosVenta", "ImagenPath1", "TEXT");
                await AddColumnIfNotExistsAsync("ArticulosVenta", "ImagenPath2", "TEXT");
                await AddColumnIfNotExistsAsync("ArticulosVenta", "ImagenPath3", "TEXT");
                await AddColumnIfNotExistsAsync("ArticulosVenta", "ImagenPath4", "TEXT");

                await AddColumnIfNotExistsAsync("ArticulosGasto", "ImagenPath1", "TEXT");
                await AddColumnIfNotExistsAsync("ArticulosGasto", "ImagenPath2", "TEXT");
                await AddColumnIfNotExistsAsync("ArticulosGasto", "ImagenPath3", "TEXT");
                await AddColumnIfNotExistsAsync("ArticulosGasto", "ImagenPath4", "TEXT");

                await AddColumnIfNotExistsAsync("ArticulosEncargo", "ImagenPath1", "TEXT");
                await AddColumnIfNotExistsAsync("ArticulosEncargo", "ImagenPath2", "TEXT");
                await AddColumnIfNotExistsAsync("ArticulosEncargo", "ImagenPath3", "TEXT");
                await AddColumnIfNotExistsAsync("ArticulosEncargo", "ImagenPath4", "TEXT");

                // Copiar datos legacy de ImagenPath a ImagenPath1
                try
                {
                    await _database.ExecuteAsync("UPDATE ArticulosVenta SET ImagenPath1 = ImagenPath WHERE ImagenPath IS NOT NULL AND ImagenPath1 IS NULL");
                    await _database.ExecuteAsync("UPDATE ArticulosGasto SET ImagenPath1 = ImagenPath WHERE ImagenPath IS NOT NULL AND ImagenPath1 IS NULL");
                    await _database.ExecuteAsync("UPDATE ArticulosEncargo SET ImagenPath1 = ImagenPath WHERE ImagenPath IS NOT NULL AND ImagenPath1 IS NULL");
                }
                catch { }

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
                  LEFT JOIN DescripcionOculta d ON v.Descripcion = d.Descripcion AND d.EntityType = 'Venta'
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
                  LEFT JOIN DescripcionOculta d ON g.Descripcion = d.Descripcion AND d.EntityType = 'Gasto'
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
                  LEFT JOIN DescripcionOculta d ON e.Descripcion = d.Descripcion AND d.EntityType = 'Encargo'
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
                  LEFT JOIN DescripcionOculta d ON e.Nombre = d.Descripcion AND d.EntityType = 'EncargoNombre'
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

        public async Task<List<string>> GetDistinctArticulosGastoDescriptionsAsync()
        {
            if (_database is null)
                throw new InvalidOperationException("La base de datos no está inicializada.");

            var descriptions = await _database.QueryAsync<ArticuloGasto>(
                @"SELECT DISTINCT a.Descripcion
                  FROM ArticulosGasto a
                  LEFT JOIN DescripcionOculta d ON a.Descripcion = d.Descripcion AND d.EntityType = 'ArticuloGasto'
                  WHERE a.Descripcion IS NOT NULL
                    AND a.Descripcion != ''
                    AND d.Id IS NULL
                  ORDER BY a.Descripcion");

            return descriptions
                .Where(a => a.Descripcion is not null)
                .Select(a => a.Descripcion!)
                .ToList();
        }

        public async Task<List<string>> GetDistinctArticulosVentaDescriptionsAsync()
        {
            if (_database is null)
                throw new InvalidOperationException("La base de datos no está inicializada.");

            var descriptions = await _database.QueryAsync<ArticuloVenta>(
                @"SELECT DISTINCT a.Descripcion
                  FROM ArticulosVenta a
                  LEFT JOIN DescripcionOculta d ON a.Descripcion = d.Descripcion AND d.EntityType = 'ArticuloVenta'
                  WHERE a.Descripcion IS NOT NULL
                    AND a.Descripcion != ''
                    AND d.Id IS NULL
                  ORDER BY a.Descripcion");

            return descriptions
                .Where(a => a.Descripcion is not null)
                .Select(a => a.Descripcion!)
                .ToList();
        }

        public async Task<List<string>> GetDistinctArticulosEncargoDescriptionsAsync()
        {
            if (_database is null)
                throw new InvalidOperationException("La base de datos no está inicializada.");

            var descriptions = await _database.QueryAsync<ArticuloEncargo>(
                @"SELECT DISTINCT a.Descripcion
                  FROM ArticulosEncargo a
                  LEFT JOIN DescripcionOculta d ON a.Descripcion = d.Descripcion AND d.EntityType = 'ArticuloEncargo'
                  WHERE a.Descripcion IS NOT NULL
                    AND a.Descripcion != ''
                    AND d.Id IS NULL
                  ORDER BY a.Descripcion");

            return descriptions
                .Where(a => a.Descripcion is not null)
                .Select(a => a.Descripcion!)
                .ToList();
        }

        public async Task DismissAutocompleteDescriptionAsync(string descripcion, string entityType)
        {
            if (_database is null)
                throw new InvalidOperationException("La base de datos no está inicializada.");

            if (string.IsNullOrWhiteSpace(descripcion))
                return;

            // Solo inserta si no existe ya para esta entidad
            var existe = await _database.Table<DescripcionOculta>()
                .FirstOrDefaultAsync(d => d.Descripcion == descripcion && d.EntityType == entityType);
            if (existe is null)
            {
                await _database.InsertAsync(new DescripcionOculta
                {
                    Descripcion = descripcion,
                    EntityType = entityType
                });
            }
        }

        public async Task ReinstateAutocompleteDescriptionAsync(string descripcion, string entityType)
        {
            if (_database is null)
                throw new InvalidOperationException("La base de datos no está inicializada.");

            if (string.IsNullOrWhiteSpace(descripcion))
                return;

            var existente = await _database.Table<DescripcionOculta>()
                .FirstOrDefaultAsync(d => d.Descripcion == descripcion && d.EntityType == entityType);
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

        // ===== MIGRACIÓN DE DATOS LEGACY =====

        /// <summary>
        /// Migra datos legacy de Ventas a ArticulosVenta si aún no se ha ejecutado.
        /// </summary>
        private async Task MigrateLegacyVentasAsync()
        {
            if (_database is null)
                return;

            try
            {
                // Migrar SOLO ventas que aún no tienen artículo asociado
                var migrated = await _database.ExecuteAsync(
                    @"INSERT INTO ArticulosVenta (VentaId, Descripcion, PrecioUnitario, Cantidad, Orden, CreatedAt, UpdatedAt, IsDeleted)
                      SELECT v.Id, v.Descripcion, v.Precio, v.Cantidad, 1, v.CreatedAt, v.UpdatedAt, v.IsDeleted
                      FROM Ventas v
                      WHERE v.IsDeleted != 1
                        AND NOT EXISTS (SELECT 1 FROM ArticulosVenta a WHERE a.VentaId = v.Id)");

                if (migrated > 0)
                    System.Diagnostics.Debug.WriteLine($"[MIGRATION] Migradas {migrated} ventas legacy a ArticulosVenta");
                else
                    System.Diagnostics.Debug.WriteLine($"[MIGRATION] No hay ventas legacy por migrar");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MIGRATION] Error migrando Ventas: {ex.Message}");
            }
        }

        /// <summary>
        /// Migra datos legacy de Gastos a ArticulosGasto si aún no se ha ejecutado.
        /// </summary>
        private async Task MigrateLegacyGastosAsync()
        {
            if (_database is null)
                return;

            try
            {
                // Migrar SOLO gastos que aún no tienen artículo asociado
                var migrated = await _database.ExecuteAsync(
                    @"INSERT INTO ArticulosGasto (GastoId, Descripcion, PrecioUnitario, Cantidad, Orden, CreatedAt, UpdatedAt, IsDeleted)
                      SELECT g.Id, g.Descripcion, g.Monto, g.Cantidad, 1, g.CreatedAt, g.UpdatedAt, g.IsDeleted
                      FROM Gastos g
                      WHERE g.IsDeleted != 1
                        AND NOT EXISTS (SELECT 1 FROM ArticulosGasto a WHERE a.GastoId = g.Id)");

                if (migrated > 0)
                    System.Diagnostics.Debug.WriteLine($"[MIGRATION] Migrados {migrated} gastos legacy a ArticulosGasto");
                else
                    System.Diagnostics.Debug.WriteLine($"[MIGRATION] No hay gastos legacy por migrar");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MIGRATION] Error migrando Gastos: {ex.Message}");
            }
        }

        /// <summary>
        /// Migra datos legacy de Encargos a ArticulosEncargo si aún no se ha ejecutado.
        /// </summary>
        private async Task MigrateLegacyEncargosAsync()
        {
            if (_database is null)
                return;

            try
            {
                // Migrar SOLO encargos que aún no tienen artículo asociado
                var migrated = await _database.ExecuteAsync(
                    @"INSERT INTO ArticulosEncargo (EncargoId, Descripcion, PrecioUnitario, Cantidad, Orden, CreatedAt, UpdatedAt, IsDeleted)
                      SELECT e.Id, e.Descripcion, e.Precio, e.Cantidad, 1, e.CreatedAt, e.UpdatedAt, e.IsDeleted
                      FROM Encargo e
                      WHERE e.IsDeleted != 1
                        AND NOT EXISTS (SELECT 1 FROM ArticulosEncargo a WHERE a.EncargoId = e.Id)");

                if (migrated > 0)
                    System.Diagnostics.Debug.WriteLine($"[MIGRATION] Migrados {migrated} encargos legacy a ArticulosEncargo");
                else
                    System.Diagnostics.Debug.WriteLine($"[MIGRATION] No hay encargos legacy por migrar");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MIGRATION] Error migrando Encargos: {ex.Message}");
            }
        }

        // ===== ARTÍCULOS (Generic Repository Factory) =====

        public IArticleRepository<T> GetArticleRepository<T>() where T : ArticuloBase, new()
        {
            if (_database is null)
                throw new InvalidOperationException("La base de datos no está inicializada.");

            var fkColumn = typeof(T) switch
            {
                Type t when t == typeof(ArticuloVenta) => "VentaId",
                Type t when t == typeof(ArticuloGasto) => "GastoId",
                Type t when t == typeof(ArticuloEncargo) => "EncargoId",
                _ => throw new NotSupportedException($"Entity type {typeof(T).Name} not supported")
            };
            return new ArticleRepository<T>(_database, fkColumn);
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
