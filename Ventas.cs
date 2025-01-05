using SQLite;

namespace Mercader
{
    [Table("Ventas")]
    public class Ventas
    {

        [PrimaryKey, AutoIncrement, Unique]
        public required int Id { get; set; }

        [Column("Precio")]
        public required decimal Precio { get; set; }

        [Column("Cantidad")]
        public required decimal Cantidad { get; set; }

        [Column("Descripcion")]
        public required string Descripcion { get; set; }

        [Column("Fecha")]
        public required DateTime Fecha { get; set; }
    }
}
