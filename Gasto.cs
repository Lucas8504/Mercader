using SQLite;
using static Mercader.ViewModels.MainViewModel;

namespace Mercader
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
    }
}
