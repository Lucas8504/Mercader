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

    [SQLite.Column("ImagenPath1")]
    public string? ImagenPath1 { get; set; }

    [SQLite.Column("ImagenPath2")]
    public string? ImagenPath2 { get; set; }

    [SQLite.Column("ImagenPath3")]
    public string? ImagenPath3 { get; set; }

    [SQLite.Column("ImagenPath4")]
    public string? ImagenPath4 { get; set; }

    // Legacy property for backward compatibility
    [NotMapped]
    public string? ImagenPath
    {
        get => ImagenPath1;
        set => ImagenPath1 = value;
    }

    [NotMapped]
    public decimal Total => PrecioUnitario * Cantidad;

    [NotMapped]
    public string TotalFormateado => Total.ToString("N2");

    [NotMapped]
    public string UnidadTexto => Cantidad == 1 ? " ud." : " uds.";

    // Helper methods
    public string? GetImagePath(int slotIndex) => slotIndex switch
    {
        0 => ImagenPath1,
        1 => ImagenPath2,
        2 => ImagenPath3,
        3 => ImagenPath4,
        _ => null
    };

    public void SetImageAtSlot(int slotIndex, string path)
    {
        switch (slotIndex)
        {
            case 0: ImagenPath1 = path; break;
            case 1: ImagenPath2 = path; break;
            case 2: ImagenPath3 = path; break;
            case 3: ImagenPath4 = path; break;
        }
    }

    public void ClearSlot(int slotIndex)
    {
        switch (slotIndex)
        {
            case 0: ImagenPath1 = null; break;
            case 1: ImagenPath2 = null; break;
            case 2: ImagenPath3 = null; break;
            case 3: ImagenPath4 = null; break;
        }
    }

    public int GetFirstEmptySlot()
    {
        if (string.IsNullOrWhiteSpace(ImagenPath1)) return 0;
        if (string.IsNullOrWhiteSpace(ImagenPath2)) return 1;
        if (string.IsNullOrWhiteSpace(ImagenPath3)) return 2;
        if (string.IsNullOrWhiteSpace(ImagenPath4)) return 3;
        return -1; // All slots full
    }

    public bool HasEmptySlot => GetFirstEmptySlot() != -1;

    public int ImageCount => new[] { ImagenPath1, ImagenPath2, ImagenPath3, ImagenPath4 }
                              .Count(p => !string.IsNullOrWhiteSpace(p));

    // Update HasImage
    [NotMapped]
    public bool HasImage => ImageCount > 0;
}