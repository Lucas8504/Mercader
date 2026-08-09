namespace Mercader.Services.Interfaces;

public interface IImageStorageService
{
    Task<string?> CompressAndSaveAsync(Stream imageStream, CancellationToken ct = default);
    Task<bool> DeleteFileAsync(string filePath);
    Task<bool> FileExistsAsync(string filePath);
}