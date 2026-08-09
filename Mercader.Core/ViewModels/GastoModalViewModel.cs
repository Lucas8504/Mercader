using Mercader.Data.Interfaces;
using Mercader.Domain.Entities;

namespace Mercader.Core.ViewModels;

/// <summary>
/// ViewModel for GastoModal - manages gasto articles.
/// </summary>
public partial class GastoModalViewModel : BaseArticuloViewModel<ArticuloGasto>
{
    public GastoModalViewModel(IArticleRepository<ArticuloGasto> articleRepository)
        : base(articleRepository) { }

    protected override void SetParentId(ArticuloGasto articulo, int parentId)
    {
        articulo.GastoId = parentId;
    }
}