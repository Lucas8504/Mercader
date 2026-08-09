namespace Mercader.Domain.Entities;

public interface IArticulo
{
    string? Descripcion { get; set; }
    decimal PrecioUnitario { get; set; }
    decimal Cantidad { get; set; }
    int Orden { get; set; }
    string? ImagenPath { get; set; }
    decimal Total { get; }
    string TotalFormateado { get; }
    string UnidadTexto { get; }
    bool HasImage { get; }
}