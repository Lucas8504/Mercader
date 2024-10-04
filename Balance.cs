using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mercader
{
    public class Balance
    {
        public List<Ventas> Ventas { get; set; } = new List<Ventas>();
        public List<Gasto> Gastos { get; set; } = new List<Gasto>();

        public decimal CalcularGanancias()
        {
            decimal totalVentas = Ventas.Sum(v => v.Encargo.Precio * v.Cantidad);
            decimal totalGastos = Gastos.Sum(g => g.Monto);
            return totalVentas - totalGastos;
        }
    }
}
