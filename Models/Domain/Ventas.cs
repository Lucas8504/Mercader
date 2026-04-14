using SQLite;

namespace Mercader.Models.Domain
{
    [Table("Ventas")]
    public class Ventas : IFecha
    {
        [PrimaryKey, AutoIncrement, Unique]
        public int Id { get; set; }

        [Column("Precio")]
        public decimal Precio { get; set; }

        [Column("Cantidad")]
        public decimal Cantidad { get; set; }

        [Column("Descripcion")]
        public string? Descripcion { get; set; }

        [Column("Fecha")]
        public DateTime Fecha { get; set; }

        // Propiedades calculadas (para UI, no se mapean a SQLite)
        public decimal Total => Precio * Cantidad;
        public string TotalFormateado => (Precio * Cantidad).ToString("N0");
    }
}
