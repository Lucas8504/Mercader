namespace Mercader
{
    internal class ComparadorBalances
    {
        public List<Balance> BalancesMensuales { get; set; } = new List<Balance>();

        public void CompararBalances()
        {
            foreach (var balance in BalancesMensuales)
            {
                Console.WriteLine($"Ganancias del mes: {balance.CalcularGanancias()}");
            }
        }
    }
}
