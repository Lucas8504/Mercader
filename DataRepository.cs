using SQLite;
namespace Mercader
{
    public sealed class DataRepository
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
        // Métodos para guardar datos
        public async Task<int> SaveEncargoAsync(Encargo encargo)
        {
            ArgumentNullException.ThrowIfNull(encargo);

            if (_database is null)
            {
                throw new InvalidOperationException("La base de datos no está inicializada.");
            }
            return encargo.Id != 0 ?
                await _database.UpdateAsync(encargo):
                await _database.InsertAsync(encargo);

        }

        public async Task<int> SaveVentasAsync(Ventas ventas)
        {
            ArgumentNullException.ThrowIfNull(ventas);

            await _semaphore.WaitAsync();
            try
            {
                if (_database is null)
                {
                    throw new InvalidOperationException("Base de datos no inicializada");
                }
                return ventas.Id != 0
                ? await _database.UpdateAsync(ventas)
                : await _database.InsertAsync(ventas);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        // Implementa el patrón IDisposable para limpiar recursos
        public void Dispose()
        {
            _semaphore.Dispose();
            _database?.CloseAsync().Wait();
        }

        public async Task<int> SaveGastoAsync(Gasto gasto)
        {
            ArgumentNullException.ThrowIfNull(gasto);

            if (_database is null)
            {
                throw new InvalidOperationException("La base de datos no está inicializada.");
            }
            return gasto.Id != 0 ?
            await _database.UpdateAsync(gasto) :
            await _database.InsertAsync(gasto);
        }

        // Métodos para obtener datos
        public async Task<List<Encargo>> GetEncargosAsync()
        {
            if (_database is null)
            {
                throw new InvalidOperationException("La base de datos no está inicializada.");
            }

            return await _database.Table<Encargo>().ToListAsync();
        }

        public async Task<List<Ventas>> GetVentasAsync()
        {
            if (_database is null)
            {
                throw new InvalidOperationException("La base de datos no está inicializada.");
            }
            return await _database.Table<Ventas>().ToListAsync();

        }

        public async Task<List<Gasto>> GetGastosAsync()
        {

            if (_database is null)
            {
                throw new InvalidOperationException("La base de datos no está inicializada.");
            }
            return await _database.Table<Gasto>().ToListAsync();
        }

        // Obtener ventas de los últimos 6 meses
        public async Task<List<Ventas>> GetVentasUltimos6MesesAsync()
        {
            if (_database is null) throw new InvalidOperationException("BD no inicializada");

            DateTime fechaInicio = DateTime.Now.AddMonths(-6).Date;
            return await _database.Table<Ventas>()
                                .Where(v => v.Fecha >= fechaInicio)
                                .OrderBy(v => v.Fecha)
                                .ToListAsync();
        }

        // Obtener gastos de los últimos 6 meses
        public async Task<List<Gasto>> GetGastosUltimos6MesesAsync()
        {
            if (_database is null)
            {
                throw new InvalidOperationException("La base de datos no está inicializada.");
            }

            DateTime fechaInicio = DateTime.Now.AddMonths(-6);
            return await _database.Table<Gasto>()
                                .Where(g => g.Fecha >= fechaInicio)
                                .ToListAsync();
        }

        // Métodos para eliminar datos
        public async Task<int> DeleteEncargoAsync(Encargo encargo)
        {
            ArgumentNullException.ThrowIfNull(encargo);

            if (_database is null)
            {
                throw new InvalidOperationException("La base de datos no está inicializada.");
            }
            return await _database.DeleteAsync(encargo);
        }

        public async Task<int> DeleteVentaAsync(Ventas venta)
        {
            ArgumentNullException.ThrowIfNull(venta);

            if (_database is null)
            {
                throw new InvalidOperationException("La base de datos no está inicializada.");
            }

            return await _database.DeleteAsync(venta);

        }

        public async Task<int> DeleteGastoAsync(Gasto gasto)
        {
            ArgumentNullException.ThrowIfNull(gasto);

            if (_database is null)
            {
                throw new InvalidOperationException("La base de datos no está inicializada.");
            }
            return await _database.DeleteAsync(gasto);
        }
    }

}