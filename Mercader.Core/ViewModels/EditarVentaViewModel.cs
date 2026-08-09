using CommunityToolkit.Mvvm.ComponentModel;
using Mercader.Data.Interfaces;
using Mercader.Domain.Entities;

namespace Mercader.Core.ViewModels;

/// <summary>
/// ViewModel for EditarVentaPage - manages editing venta articles.
/// </summary>
public partial class EditarVentaViewModel : BaseArticuloViewModel<ArticuloVenta>
{
    [ObservableProperty]
    private string _descripcion = string.Empty;

    public EditarVentaViewModel(IArticleRepository<ArticuloVenta> articleRepository)
        : base(articleRepository) { }

    protected override void SetParentId(ArticuloVenta articulo, int parentId)
    {
        articulo.VentaId = parentId;
    }

    /// <summary>
    /// Loads the venta and its articles for editing.
    /// </summary>
    public async Task LoadAsync(Ventas venta)
    {
        Descripcion = venta.Descripcion;
        await LoadArticulosAsync(venta.Id);
    }

    /// <summary>
    /// Saves the venta and its articles.
    /// </summary>
    public async Task<bool> SaveAsync(Ventas venta, IDataRepository repository)
    {
        if (string.IsNullOrWhiteSpace(Descripcion))
            return false;

        if (!HasArticulos || !AllArticulosHaveDescription)
            return false;

        venta.Descripcion = Descripcion;
        venta.Precio = Total;
        venta.Cantidad = 1;

        await repository.SaveVentasAsync(venta);
        await SaveArticulosAsync(venta.Id, deleteExisting: true);
        return true;
    }
}