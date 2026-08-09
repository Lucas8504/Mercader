using SQLite;
using Mercader.Domain.Entities;

namespace Mercader.Domain.Entities;

[Table("ArticulosEncargo")]
public class ArticuloEncargo : ArticuloBase
{
    [Column("EncargoId")]
    public int EncargoId { get; set; }
}