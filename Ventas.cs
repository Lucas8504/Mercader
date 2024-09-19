using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mercader
{
    internal class Ventas
    {
        public Encargo Encargo { get; set; }
        public int Cantidad { get; set; }
        public DateTime Fecha { get; set; }
    }
}
