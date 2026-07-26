using SQLite;

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
        [Ignore] public string DesgloseLinea1 => TieneArticulos && Articulos.Count > 0
            ? $"{Articulos[0].PrecioUnitario:N2} x {Articulos[0].Cantidad}{Articulos[0].UnidadTexto}"
            : $"{Monto:N2} x {Cantidad}{UnidadTexto}";
        [Ignore] public string DesgloseLinea2 => TieneArticulos && Articulos.Count > 1
            ? $"{Articulos[1].PrecioUnitario:N2} x {Articulos[1].Cantidad}{Articulos[1].UnidadTexto}"
            : string.Empty;
        [Ignore] public string DesgloseLinea3 => TieneArticulos && Articulos.Count > 2
            ? $"{Articulos[2].PrecioUnitario:N2} x {Articulos[2].Cantidad}{Articulos[2].UnidadTexto}"
            : string.Empty;
        [Ignore] public string DesgloseLinea4 => TieneArticulos && Articulos.Count > 3
            ? $"{Articulos[3].PrecioUnitario:N2} x {Articulos[3].Cantidad}{Articulos[3].UnidadTexto}"
            : string.Empty;
        [Ignore] public string DesgloseLinea5 => TieneArticulos && Articulos.Count > 4
            ? $"{Articulos[4].PrecioUnitario:N2} x {Articulos[4].Cantidad}{Articulos[4].UnidadTexto}"
            : string.Empty;
    }
}
