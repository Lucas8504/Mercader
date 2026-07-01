using SQLite;

namespace Mercader.Domain.Entities
{
    /// <summary>
    /// Entidad base abstracta con campos comunes a todas las entidades.
    /// Provee Id, timestamps y soft-delete.
    /// </summary>
    public abstract class BaseEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("UpdatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [Column("IsDeleted")]
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Marca la entidad como eliminada (soft-delete).
        /// </summary>
        public void SoftDelete()
        {
            IsDeleted = true;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Marca que la entidad fue modificada.
        /// </summary>
        public void MarkUpdated()
        {
            UpdatedAt = DateTime.UtcNow;
        }
    }
}