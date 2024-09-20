namespace Mercader
{
    public partial class MainPage : ContentPage
    {
        

        public MainPage()
        {
            InitializeComponent();
        }


        private void OnAgregarEncargoClicked(object sender, EventArgs e)
        {
            var encargo = new Encargo
            {
                Nombre = EncargoEntry.Text,
                Precio = decimal.Parse(PrecioEntry.Text)
            };
            // Aquí puedes agregar el encargo a una lista si es necesario
        }

        private void OnAgregarVentaClicked(object sender, EventArgs e, Balance balance)
        {
            var venta = new Ventas
            {
                Encargo = new Encargo { Nombre = EncargoEntry.Text, Precio = decimal.Parse(PrecioEntry.Text) },
                Cantidad = int.Parse(CantidadEntry.Text),
                Fecha = DateTime.Now
            };
            balance.Ventas.Add(venta);
        }

        private void OnAgregarGastoClicked(object sender, EventArgs e, Balance balance)
        {
            var gasto = new Gasto
            {
                Descripcion = DescripcionGastoEntry.Text,
                Monto = decimal.Parse(MontoGastoEntry.Text),
                Fecha = DateTime.Now
            };
            balance.Gastos.Add(gasto);
        }

        private void OnCalcularGananciasClicked(object sender, EventArgs e, Balance balance)
        {
            var ganancias = balance.CalcularGanancias();
            GananciasLabel.Text = $"Ganancias: {ganancias:C}";
        }

    }

}
