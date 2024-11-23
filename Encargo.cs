using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mercader
{
    public class Encargo
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public required string Nombre { get; set; }
        public decimal Precio { get; set; }
    }
}
