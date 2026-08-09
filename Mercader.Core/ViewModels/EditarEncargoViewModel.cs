using CommunityToolkit.Mvvm.ComponentModel;
using Mercader.Data.Interfaces;
using Mercader.Domain.Entities;

namespace Mercader.Core.ViewModels;

/// <summary>
/// ViewModel for EditarEncargoPage - manages editing encargo articles.
/// </summary>
public partial class EditarEncargoViewModel : BaseArticuloViewModel<ArticuloEncargo>
{
    [ObservableProperty]
    private string _nombre = string.Empty;

    [ObservableProperty]
    private string _contacto = string.Empty;

    [ObservableProperty]
    private string _descripcion = string.Empty;

    [ObservableProperty]
    private DateTime _fechaEntrega = DateTime.Now;

    public EditarEncargoViewModel(IArticleRepository<ArticuloEncargo> articleRepository)
        : base(articleRepository) { }

    protected override void SetParentId(ArticuloEncargo articulo, int parentId)
    {
        articulo.EncargoId = parentId;
    }

    /// <summary>
    /// Loads the encargo and its articles for editing.
    /// </summary>
    public async Task LoadAsync(Encargo encargo)
    {
        Nombre = encargo.Nombre;
        Contacto = CleanPhoneNumber(encargo.Contacto);
        Descripcion = encargo.Descripcion ?? string.Empty;
        FechaEntrega = encargo.FechaEntrega;
        await LoadArticulosAsync(encargo.Id);
    }

    /// <summary>
    /// Saves the encargo and its articles.
    /// </summary>
    public async Task<bool> SaveAsync(Encargo encargo, IDataRepository repository)
    {
        if (string.IsNullOrWhiteSpace(Nombre))
            return false;

        if (string.IsNullOrWhiteSpace(Contacto) || !IsValidPhoneNumber(Contacto))
            return false;

        if (!HasArticulos || !AllArticulosHaveDescription)
            return false;

        encargo.Nombre = Nombre.Trim();
        encargo.Contacto = Contacto.Trim();
        encargo.Descripcion = Descripcion?.Trim();
        encargo.FechaEntrega = FechaEntrega;
        encargo.Precio = Total;
        encargo.Cantidad = 1;

        await repository.SaveEncargoAsync(encargo);
        await SaveArticulosAsync(encargo.Id, deleteExisting: true);
        return true;
    }

    // Phone validation helpers
    private bool IsValidPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        string cleanedNumber = phoneNumber.Replace(" ", "")
                                            .Replace("-", "")
                                            .Replace("(", "")
                                            .Replace(")", "")
                                            .Replace("+", "");

        return cleanedNumber.All(char.IsDigit) &&
               cleanedNumber.Length >= 7 &&
               cleanedNumber.Length <= 15;
    }

    private string CleanPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return phoneNumber;

        return new string(phoneNumber.Where(char.IsDigit).ToArray());
    }
}