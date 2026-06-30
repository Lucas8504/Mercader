using SQLite;

namespace Mercader.Domain.Entities
{
    /// <summary>
    /// Almacena descripciones/nombres que el usuario descartó del autocompletado.
    /// EntityType indica a qué entidad pertenece el descarte ("Venta", "Gasto",
    /// "Encargo", "EncargoNombre"), para evitar que descartar una descripción
    /// en una entidad la oculte también en otra.
    /// </summary>
    [Table("DescripcionOculta")]
    public class DescripcionOculta
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Column("Descripcion")]
        public string Descripcion { get; set; } = string.Empty;

        [Column("EntityType")]
        public string EntityType { get; set; } = string.Empty;
    }
}
