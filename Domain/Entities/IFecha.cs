namespace Mercader.Domain.Entities
{
    /// <summary>
    /// Interface para entidades que tienen fecha.
    /// Permite usar genericidad en servicios de filtrado/agrupación.
    /// </summary>
    public interface IFecha
    {
        DateTime Fecha { get; }
    }
}
