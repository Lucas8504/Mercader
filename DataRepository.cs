using SQLite;
namespace Mercader
{
    public class DataRepository
    {
        private readonly SQLiteAsyncConnection _database;

        public DataRepository(string dbPath)
        {
            if (string.IsNullOrEmpty(dbPath))
                throw new ArgumentNullException(nameof(dbPath));

            _database = new SQLiteAsyncConnection(dbPath);
            InitializeDatabaseAsync().Wait();
        }

        private async Task InitializeDatabaseAsync()
        {
            await _database.CreateTableAsync<Encargo>();
            await _database.CreateTableAsync<Ventas>();
            await _database.CreateTableAsync<Gasto>();
        }

        // Métodos para guardar datos
        public Task<int> SaveEncargoAsync(Encargo encargo)
        {
            if (encargo.Id != 0)
            {
                return _database.UpdateAsync(encargo);
            }
            else
            {
                return _database.InsertAsync(encargo);
            }
        }

        public Task<int> SaveVentasAsync(Ventas ventas)
        {
            if (ventas.Id != 0)
            {
                return _database.UpdateAsync(ventas);
            }
            else
            {
                return _database.InsertAsync(ventas);
            }
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