using SQLite;
using Mercader.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Mercader
{
    [Table("Encargo")]
    public class Encargo : Models.IFecha, INotifyPropertyChanged
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

        // Propiedades calculadas (no se mapean a SQLite)
        public decimal Total => Precio * Cantidad;

        public string TotalFormateado => Total.ToString("N0");

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
