using Mercader.Data.Interfaces;
using Mercader.Domain.Entities;

namespace Mercader.Core.ViewModels;

/// <summary>
/// ViewModel for EncModal - manages encargo articles.
/// </summary>
public partial class EncModalViewModel : BaseArticuloViewModel<ArticuloEncargo>
{
    public EncModalViewModel(IArticleRepository<ArticuloEncargo> articleRepository)
        : base(articleRepository) { }

    protected override void SetParentId(ArticuloEncargo articulo, int parentId)
    {
        articulo.EncargoId = parentId;
    }
}