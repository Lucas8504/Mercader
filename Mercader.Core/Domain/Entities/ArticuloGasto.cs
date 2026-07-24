using SQLite;

namespace Mercader.Domain.Entities
{
    [Table("ArticulosGasto")]
    public class ArticuloGasto : BaseEntity
    {
        [Column("GastoId")]
        public int GastoId { get; set; }

        [Column("Descripcion")]
        public string? Descripcion { get; set; }

        [Column("PrecioUnitario")]
        public decimal PrecioUnitario { get; set; }

        [Column("Cantidad")]
        public decimal Cantidad { get; set; }

        [Column("Orden")]
        public int Orden { get; set; }

        // Propiedades calculadas (para UI, no se mapean a SQLite)
        public decimal Total => PrecioUnitario * Cantidad;
        public string TotalFormateado => (PrecioUnitario * Cantidad).ToString("N0");
        public string UnidadTexto => Cantidad == 1 ? " ud." : " uds.";
    }
}
