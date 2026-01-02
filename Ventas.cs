using SQLite;
using Mercader.Models;
using static Mercader.ViewModels.MainViewModel;

namespace Mercader
{
    [Table("Ventas")]
    public class Ventas: Models.IFecha
    {

        [PrimaryKey, AutoIncrement, Unique]
        public int Id { get; set; }

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
