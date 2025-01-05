using SQLite;

namespace Mercader
{
    [Table("Gastos")]
    public class Gasto
    {

        [PrimaryKey, AutoIncrement, Unique]
        public required int Id { get; set; }

        [Column("Descripcion")]
        public required string Descripcion { get; set; }

        [Column("Monto")]
        public required decimal Monto { get; set; }

        [Column("Cantidad")]
        public required decimal Cantidad { get; set; }

        [Column("Fecha")]
        public required DateTime Fecha { get; set; }
    }
}
