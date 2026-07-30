using SQLite;
using Mercader.Models;

namespace Mercader.Domain.Entities
{
    [Table("Encargo")]
    public class Encargo : BaseEntity, IFecha
    {
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

        [Column("Estado"), MaxLength(20)]
        public string Estado { get; set; } = "PENDIENTE";

        // Propiedades calculadas (para UI, no se mapean a SQLite)
        public decimal Total => Precio * Cantidad;
        public string TotalFormateado => Total.ToString("N0");
        public string UnidadTexto => Cantidad == 1 ? " ud." : " uds.";

        // ===== MULTI-ARTÍCULO (cargado en runtime, no persistido) =====
        [Ignore] public List<ArticuloEncargo> Articulos { get; set; } = new();
        [Ignore] public bool TieneArticulos => Articulos.Count > 0;
        [Ignore] public decimal TotalCalculado => TieneArticulos ? Articulos.Sum(a => a.Total) : Precio * Cantidad;
        [Ignore] public string TotalCalculadoFormateado => TotalCalculado.ToString("N0");

        /// <summary>
        /// Desglose dinámico: un item por artículo con descripción y texto.
        /// Si no tiene artículos, muestra el formato legacy.
        /// </summary>
        [Ignore] public List<DesgloseItem> DesgloseItems
        {
            get
            {
                if (!TieneArticulos)
                {
                    return
                    [
                        new DesgloseItem
                        {
                            Descripcion = string.Empty,
                            Texto = $"{Precio:N2} x {Cantidad:N0}{UnidadTexto}"
                        }
                    ];
                }

                return Articulos.Select(a => new DesgloseItem
                {
                    Descripcion = a.Descripcion ?? string.Empty,
                    Texto = $"{a.PrecioUnitario:N2} x {a.Cantidad:N0}{a.UnidadTexto}"
                }).ToList();
            }
        }
    }
}
