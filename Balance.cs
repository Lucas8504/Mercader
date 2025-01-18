namespace Mercader
{
    public class Balance
    {
        public List<Ventas> Ventas { get; set; } = new List<Ventas>();
        public List<Gasto> Gastos { get; set; } = new List<Gasto>();
        public List<Encargo> Encargos { get; set; } = new List<Encargo>();

        public decimal CalcularGastos()
        {
            return Gastos.Sum(g => g.Monto * g.Cantidad);
        }

        public decimal CalcularVentas()
        {
            return Ventas.Sum(v => v.Precio * v.Cantidad);
        }

        public decimal CalcularEncargos()
        {
            return Encargos.Sum(e => e.Precio * e.Cantidad);
        }

        public decimal CalcularGanancias()
        {
            return CalcularVentas() - CalcularGastos();
        }
    }
}
