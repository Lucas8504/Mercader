using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mercader
{
    
    [Table("Ventas")]
    public class Ventas
    {
        [PrimaryKey, AutoIncrement]
        [Column("Id")]
        public int Id { get; set; }

        [Column("Precio")]
        public decimal precio { get; set; }

        [Column("Cantidad")]
        public decimal cantidad { get; set; }

        [Column("Fecha")]
        public DateTime fecha { get; set; }
    }
}
