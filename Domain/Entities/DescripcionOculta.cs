using SQLite;

namespace Mercader.Domain.Entities
{
    /// <summary>
    /// Almacena descripciones que el usuario descartó del autocompletado.
    /// Si se vuelve a agregar una venta con esa descripción, reappeará.
    /// </summary>
    [Table("DescripcionOculta")]
    public class DescripcionOculta
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Column("Descripcion")]
        public string Descripcion { get; set; } = string.Empty;
    }
}
