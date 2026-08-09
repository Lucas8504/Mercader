using CommunityToolkit.Mvvm.ComponentModel;
using Mercader.Data.Interfaces;
using Mercader.Domain.Entities;

namespace Mercader.Core.ViewModels;

/// <summary>
/// ViewModel for EditarGastoPage - manages editing gasto articles.
/// </summary>
public partial class EditarGastoViewModel : BaseArticuloViewModel<ArticuloGasto>
{
    [ObservableProperty]
    private string _descripcion = string.Empty;

    public EditarGastoViewModel(IArticleRepository<ArticuloGasto> articleRepository)
        : base(articleRepository) { }

    protected override void SetParentId(ArticuloGasto articulo, int parentId)
    {
        articulo.GastoId = parentId;
    }

    /// <summary>
    /// Loads the gasto and its articles for editing.
    /// </summary>
    public async Task LoadAsync(Gasto gasto)
    {
        Descripcion = gasto.Descripcion;
        await LoadArticulosAsync(gasto.Id);
    }

    /// <summary>
    /// Saves the gasto and its articles.
    /// </summary>
    public async Task<bool> SaveAsync(Gasto gasto, IDataRepository repository)
    {
        if (string.IsNullOrWhiteSpace(Descripcion))
            return false;

        if (!HasArticulos || !AllArticulosHaveDescription)
            return false;

        gasto.Descripcion = Descripcion;
        gasto.Monto = Total;
        gasto.Cantidad = 1;

        await repository.SaveGastoAsync(gasto);
        await SaveArticulosAsync(gasto.Id, deleteExisting: true);
        return true;
    }
}