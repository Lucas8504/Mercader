using SQLite;
using Mercader.Domain.Entities;

namespace Mercader.Domain.Entities;

[Table("ArticulosGasto")]
public class ArticuloGasto : ArticuloBase
{
    [Column("GastoId")]
    public int GastoId { get; set; }
}