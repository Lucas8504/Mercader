using SQLite;
using Mercader.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Mercader
{
    [Table("Gastos")]
    public class Gasto : IFecha, INotifyPropertyChanged
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

        // Propiedades calculadas (no se mapean a SQLite)
        public decimal Total => Monto * Cantidad;

        public string TotalFormateado => Total.ToString("N0");

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
