using SQLite;

namespace Mercader
{
    [Table("Encargo")]
    public class Encargo
    {
        [PrimaryKey, AutoIncrement, Unique]
        public required int Id { get; set; }

        [Column("Nombre"), MaxLength(360), Unique]
        public required string Nombre { get; set; }

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
