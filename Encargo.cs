using SQLite;
using static Mercader.ViewModels.MainViewModel;

namespace Mercader
{
    [Table("Encargo")]
    public class Encargo : IFecha
    {
        [PrimaryKey, AutoIncrement, Unique]
        public int Id { get; set; }

        [Column("Nombre"), MaxLength(360)]
        public string? Nombre { get; set; }

        [Column("Contacto"), MaxLength(360)]
        public string Contacto { get; set; } = string.Empty;

        [Column("Precio")]
        public decimal Precio { get; set; }

        [Column("Cantidad")]
        public decimal Cantidad { get; set; }

        [Column("Descripcion")]
        public string? Descripcion { get; set; }

        [Column("Fecha")]
        public DateTime Fecha { get; set; }

        [Column("FechaEntrega")]
        public DateTime FechaEntrega { get; set; }

    }
}
