using SQLite;
namespace Mercader
{
    public class DataRepository
    {
        private SQLiteAsyncConnection? _database;
        private readonly string _dbPath;

        public DataRepository(string dbPath)
        {
            _dbPath = dbPath ?? throw new ArgumentNullException(nameof(dbPath));
        }

        public async Task InitializeDatabaseAsync()
        {
            try
            {
                if (_database is not null)
                    return;

                _database = new SQLiteAsyncConnection(_dbPath);

                await _database.CreateTableAsync<Encargo>();
                await _database.CreateTableAsync<Ventas>();
                await _database.CreateTableAsync<Gasto>();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error al inicializar la base de datos", ex);
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

        public Task<int> SaveVentasAsync(Ventas ventas)
        {
            ArgumentNullException.ThrowIfNull(ventas);

            if (_database is null)
            {
                throw new InvalidOperationException("La base de datos no está inicializada.");
            }

            return ventas.Id != 0 ?
                _database.UpdateAsync(ventas) :
                _database.InsertAsync(ventas);
        }

        public Task<int> SaveGastoAsync(Gasto gasto)
        {
            if (gasto.Id != 0)
            {
                return _database.UpdateAsync(gasto);
            }
            else
            {
                return _database.InsertAsync(gasto);
            }
        }

        // Métodos para obtener datos
        public Task<List<Encargo>> GetEncargosAsync()
        {
            return _database.Table<Encargo>().ToListAsync();
        }

        public Task<List<Ventas>> GetVentasAsync()
        {
            return _database.Table<Ventas>().ToListAsync();
        }

        public Task<List<Gasto>> GetGastosAsync()
        {

            return _database.Table<Gasto>().ToListAsync();
        }

        // Métodos para eliminar datos
        public Task<int> DeleteEncargoAsync(Encargo encargo)
        {
            return _database.DeleteAsync(encargo);
        }

        public Task<int> DeleteVentaAsync(Ventas venta)
        {
            return _database.DeleteAsync(venta);
        }

        public Task<int> DeleteGastoAsync(Gasto gasto)
        {
            return _database.DeleteAsync(gasto);
        }
    }

}