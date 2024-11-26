using SQLite;
using System.Threading.Tasks;

namespace Mercader
{
    [Table("Encargos")]
    public class Encargo
    {
        [PrimaryKey, AutoIncrement]
        [Column("Id")]
        public int Id { get; set; }

        [Column("Precio")]
        public decimal precio { get; set; }

        [Column("Cantidad")]
        public decimal cantidad { get; set; }

        [Column("Descripcion")]
        public string? Descripcion { get; set; }

        [Column("Fecha")]
        public DateTime Fecha { get; set; }
    }
}
