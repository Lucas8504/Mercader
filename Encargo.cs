using SQLite;

namespace Mercader
{
    [Table("Encargo")]
    public class Encargo
    {
        [PrimaryKey, AutoIncrement, Unique]
        public int Id { get; set; }

        [Column("Nombre"), MaxLength(360), Unique]
        public string? Nombre { get; set; }

        [Column("Precio")]
        public decimal Precio { get; set; }

        [Column("Cantidad")]
        public decimal Cantidad { get; set; }

        [Column("Descripcion")]
        public string? Descripcion { get; set; }

        [Column("Fecha")]
        public DateTime Fecha { get; set; }

    }
}
