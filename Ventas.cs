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

        [Column("Precio")]
        public decimal Precio { get; set; }

        [Column("Cantidad")]
        public decimal Cantidad { get; set; }

        [Column("Descripcion")]
        public required string Descripcion { get; set; }

        [Column("Fecha")]
        public DateTime Fecha { get; set; }
    }
}
