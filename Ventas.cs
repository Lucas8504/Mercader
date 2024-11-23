using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mercader
{
    public class Ventas
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public decimal Precio { get; set; }
        public decimal Cantidad { get; set; }
        public required string Descripcion { get; set; }
        public DateTime Fecha { get; set; }
    }
}
