using SQLite;

namespace Mercader.Domain.Entities
{
    [Table("Gastos")]
    public class Gasto : BaseEntity, IFecha
    {
        [Column("Descripcion")]
        public string? Descripcion { get; set; }

        [Column("Monto")]
        public decimal Monto { get; set; }

        [Column("Cantidad")]
        public decimal Cantidad { get; set; }

        [Column("Fecha")]
        public DateTime Fecha { get; set; }

        // Propiedades calculadas (para UI, no se mapean a SQLite)
        public decimal Total => Monto * Cantidad;
        public string TotalFormateado => (Monto * Cantidad).ToString("N0");
        public string UnidadTexto => Cantidad == 1 ? " ud." : " uds.";
    }
}
