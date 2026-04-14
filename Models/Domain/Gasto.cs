using SQLite;

namespace Mercader.Models.Domain
{
    [Table("Gastos")]
    public class Gasto : IFecha
    {
        [PrimaryKey, AutoIncrement, Unique]
        public int Id { get; set; }

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
    }
}
