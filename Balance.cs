namespace Mercader
{
    public class Balance
    {
        public List<Ventas> Ventas { get; set; } = new List<Ventas>();
        public List<Gasto> Gastos { get; set; } = new List<Gasto>();
        public List<Encargo> Encargos { get; set; } = new List<Encargo>();

        public decimal CalcularGastos()
        {
            try
            {
                // Intenta calcular la suma de gastos
                return Gastos.Sum(g => g.Monto * g.Cantidad);
            }
            catch (Exception)
            {
                // Si hay error (ej: Gastos es null, Monto/Cantidad inválidos), retorna 0
                return 0m; // El sufijo "m" indica que es un decimal
            }
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
