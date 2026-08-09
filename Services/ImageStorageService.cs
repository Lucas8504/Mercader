using SkiaSharp;
using Microsoft.Maui.Storage;
using Mercader.Services.Interfaces;

namespace Mercader.Services;

/// <summary>
/// Servicio para almacenamiento y compresión de imágenes.
/// Usa SkiaSharp para comprimir a JPEG quality 80, max 800px lado más largo.
/// Guarda en FileSystem.AppDataDirectory/Images/{Guid}.jpg
/// </summary>
public sealed class ImageStorageService : IImageStorageService
{
    private readonly string _imagesDirectory;

    public ImageStorageService()
    {
        _imagesDirectory = Path.Combine(FileSystem.AppDataDirectory, "Images");
        if (!Directory.Exists(_imagesDirectory))
        {
            Directory.CreateDirectory(_imagesDirectory);
        }
    }

    /// <inheritdoc />
    public async Task<string?> CompressAndSaveAsync(Stream imageStream, CancellationToken ct = default)
    {
        if (imageStream is null || imageStream.Length == 0)
            return null;

        try
        {
            // Leer imagen original con SkiaSharp
            using var originalBitmap = SKBitmap.Decode(imageStream);
            if (originalBitmap is null)
                return null;

            // Calcular nuevas dimensiones (max 800px en el lado más largo)
            var (newWidth, newHeight) = CalculateResizedDimensions(
                originalBitmap.Width,
                originalBitmap.Height,
                800);

            // Redimensionar
            using var resizedBitmap = originalBitmap.Resize(new SKImageInfo(newWidth, newHeight), new SKSamplingOptions(SKFilterMode.Linear));
            if (resizedBitmap is null)
                return null;

            // Codificar a JPEG quality 80
            using var image = SKImage.FromBitmap(resizedBitmap);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, 80);
            if (data is null)
                return null;

            // Generar nombre único
            var fileName = $"{Guid.NewGuid()}.jpg";
            var filePath = Path.Combine(_imagesDirectory, fileName);

            // Guardar archivo
            await using var fileStream = File.Create(filePath);
            data.SaveTo(fileStream);
            await fileStream.FlushAsync(ct);

            return filePath;
        }
        catch (Exception)
        {
            // Error de decodificación, compresión o escritura - devolver null
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteFileAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        try
        {
            if (File.Exists(filePath))
            {
                await Task.Run(() => File.Delete(filePath));
                return true;
            }
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        return await Task.Run(() => File.Exists(filePath));
    }

    /// <summary>
    /// Calcula las nuevas dimensiones manteniendo aspect ratio, máximo maxDimension en el lado más largo.
    /// </summary>
    private static (int width, int height) CalculateResizedDimensions(int originalWidth, int originalHeight, int maxDimension)
    {
        if (originalWidth <= maxDimension && originalHeight <= maxDimension)
            return (originalWidth, originalHeight);

        double ratio;
        if (originalWidth > originalHeight)
        {
            ratio = (double)maxDimension / originalWidth;
        }
        else
        {
            ratio = (double)maxDimension / originalHeight;
        }

        var newWidth = (int)Math.Round(originalWidth * ratio);
        var newHeight = (int)Math.Round(originalHeight * ratio);

        return (newWidth, newHeight);
    }
}