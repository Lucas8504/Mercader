using Mercader.Data.Interfaces;
using Mercader.Domain.Entities;

namespace Mercader.Core.ViewModels;

/// <summary>
/// ViewModel for VentaModal - manages venta articles.
/// </summary>
public partial class VentaModalViewModel : BaseArticuloViewModel<ArticuloVenta>
{
    public VentaModalViewModel(IArticleRepository<ArticuloVenta> articleRepository)
        : base(articleRepository) { }

    protected override void SetParentId(ArticuloVenta articulo, int parentId)
    {
        articulo.VentaId = parentId;
    }
}