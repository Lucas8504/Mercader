using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mercader
{
    public class Balance
    {
        public List<Ventas> Ventas { get; set; } = [];
        public List<Gasto> Gastos { get; set; } = [];

        public decimal CalcularGanancias()
        {
            decimal totalVentas = Ventas.Sum(v => v.Precio * v.Cantidad);
            decimal totalGastos = Gastos.Sum(g => g.Monto);
            return totalVentas - totalGastos;
        }

        public decimal CalcularVentas()
        {
            decimal totalVentas = 0;
            foreach (var venta in Ventas)
            {
                totalVentas += venta.Precio * venta.Cantidad;
            }
            return totalVentas;
        }
    }
}
