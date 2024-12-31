namespace Mercader
{
    public class Balance
    {
        public List<Ventas> Ventas { get; set; } = [];
        public List<Gasto> Gastos { get; set; } = [];
        public List<Encargo> Encargos { get; set; } = [];


        public decimal CalcularGastos()
        {
            decimal totalGastos = 0;
            foreach (var gasto in Gastos)
            {
                totalGastos += gasto.Monto;
            }
            return totalGastos;
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

        public decimal CalcularEncargos()
        {
            decimal totalEncargos = 0;
            foreach (var encargo in Encargos)
            {
                totalEncargos += encargo.Precio * encargo.Cantidad;
            }
            return totalEncargos;
        }

        public decimal CalcularGanancias()
        {
            decimal totalVentas = CalcularVentas();
            decimal totalGastos = CalcularGastos();
            return totalVentas - totalGastos;
        }
    }
}
