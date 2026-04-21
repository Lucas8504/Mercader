using SQLite;

namespace Mercader.Domain.Entities
{
    [Table("Ventas")]
    public class Ventas : BaseEntity, IFecha
    {
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
