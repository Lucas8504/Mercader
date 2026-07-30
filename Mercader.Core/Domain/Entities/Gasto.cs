using SQLite;
using Mercader.Models;

namespace Mercader.Domain.Entities
{
    [Table("Gastos")]
    public class Gasto : BaseEntity, IFecha
    {
        [Column("Descripcion")]
        public string? Descripcion { get; set; }

        [Column("Monto")]
        public decimal Monto { get; set; }

        [Column("Cantidad")]
        public decimal Cantidad { get; set; }

        [Column("Fecha")]
        public DateTime Fecha { get; set; }

        // Propiedades calculadas (para UI, no se mapean a SQLite)
        public decimal Total => Monto * Cantidad;
        public string TotalFormateado => (Monto * Cantidad).ToString("N0");
        public string UnidadTexto => Cantidad == 1 ? " ud." : " uds.";

        // ===== MULTI-ARTÍCULO (cargado en runtime, no persistido) =====
        [Ignore] public List<ArticuloGasto> Articulos { get; set; } = new();
        [Ignore] public bool TieneArticulos => Articulos.Count > 0;
        [Ignore] public decimal TotalCalculado => TieneArticulos ? Articulos.Sum(a => a.Total) : Monto * Cantidad;
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
                            Texto = $"{Monto:N2} x {Cantidad:N0}{UnidadTexto}"
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
