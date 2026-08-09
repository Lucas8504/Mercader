using SQLite;
using Mercader.Domain.Entities;

namespace Mercader.Domain.Entities;

[Table("ArticulosVenta")]
public class ArticuloVenta : ArticuloBase
{
    [Column("VentaId")]
    public int VentaId { get; set; }
}