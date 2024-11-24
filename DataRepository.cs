using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;
namespace Mercader
{
    public class DataRepository
    {
        private readonly SQLiteAsyncConnection _database;

        public DataRepository(string dbPath)
        {
            _database = new SQLiteAsyncConnection(dbPath);
            _database.CreateTableAsync<Encargo>().Wait();
            _database.CreateTableAsync<Ventas>().Wait();
            _database.CreateTableAsync<Gasto>().Wait();
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

        public Task<int> SaveVentaAsync(Ventas ventas)
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

        public Task<List<Venta>> GetVentasAsync()
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

        public Task<int> DeleteVentaAsync(Venta venta)
        {
            return _database.DeleteAsync(venta);
        }

        public Task<int> DeleteGastoAsync(Gasto gasto)
        {
            return _database.DeleteAsync(gasto);
        }
    }

}
