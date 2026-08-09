using SQLite;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mercader.Domain.Entities;

public abstract class ArticuloBase : BaseEntity, IArticulo
{
    [SQLite.Column("Descripcion")]
    public string? Descripcion { get; set; }

    [SQLite.Column("PrecioUnitario")]
    public decimal PrecioUnitario { get; set; }

    [SQLite.Column("Cantidad")]
    public decimal Cantidad { get; set; }

    [SQLite.Column("Orden")]
    public int Orden { get; set; }

    [SQLite.Column("ImagenPath")]
    public string? ImagenPath { get; set; }

    [NotMapped]
    public decimal Total => PrecioUnitario * Cantidad;

    [NotMapped]
    public string TotalFormateado => Total.ToString("N2");

    [NotMapped]
    public string UnidadTexto => Cantidad == 1 ? " ud." : " uds.";

    [NotMapped]
    public bool HasImage => !string.IsNullOrWhiteSpace(ImagenPath);
}