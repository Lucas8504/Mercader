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

        [Column("Fecha")]
        public DateTime Fecha { get; set; }
    }
}
