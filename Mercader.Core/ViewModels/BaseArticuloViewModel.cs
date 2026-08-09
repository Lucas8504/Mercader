using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mercader.Data.Interfaces;
using Mercader.Domain.Entities;

namespace Mercader.Core.ViewModels;

/// <summary>
/// Base ViewModel for managing articles (Articulos) across different entity types.
/// Contains all shared logic for: add, remove, reorder, total calculation, validation, and article persistence.
/// Image handling and autocomplete UI logic remain in code-behind (which has MAUI access).
/// Dismiss autocomplete is handled in code-behind via IDataRepository.
/// </summary>
/// <typeparam name="TArticulo">The specific article type (ArticuloGasto, ArticuloVenta, ArticuloEncargo)</typeparam>
public abstract partial class BaseArticuloViewModel<TArticulo> : ObservableObject where TArticulo : ArticuloBase, new()
{
    protected readonly IArticleRepository<TArticulo> _articleRepository;

    [ObservableProperty]
    private ObservableCollection<TArticulo> _articulos = new();

    [ObservableProperty]
    private decimal _total;

    [ObservableProperty]
    private List<string> _articuloSuggestions = new();

    [ObservableProperty]
    private bool _showArticuloSuggestions;

    private TArticulo? _currentArticulo;

    protected BaseArticuloViewModel(IArticleRepository<TArticulo> articleRepository)
    {
        _articleRepository = articleRepository;
    }

    // ===== ARTICLE MANAGEMENT COMMANDS =====

    /// <summary>
    /// Adds a new article to the collection.
    /// </summary>
    [RelayCommand]
    private void AgregarArticulo()
    {
        var articulo = new TArticulo { Orden = Articulos.Count + 1 };
        Articulos.Add(articulo);
        RecalcularTotal();
    }

    /// <summary>
    /// Removes an article from the collection.
    /// </summary>
    [RelayCommand]
    private void EliminarArticulo(TArticulo? articulo)
    {
        if (articulo == null) return;
        Articulos.Remove(articulo);
        ReordenarArticulos();
        RecalcularTotal();
    }

    /// <summary>
    /// Moves an article up in the collection.
    /// </summary>
    [RelayCommand]
    private void SubirArticulo(TArticulo? articulo)
    {
        if (articulo == null) return;
        var index = Articulos.IndexOf(articulo);
        if (index <= 0) return;
        Articulos.Move(index, index - 1);
        ReordenarArticulos();
    }

    /// <summary>
    /// Moves an article down in the collection.
    /// </summary>
    [RelayCommand]
    private void BajarArticulo(TArticulo? articulo)
    {
        if (articulo == null) return;
        var index = Articulos.IndexOf(articulo);
        if (index < 0 || index >= Articulos.Count - 1) return;
        Articulos.Move(index, index + 1);
        ReordenarArticulos();
    }

    /// <summary>
    /// Reorders all articles to have sequential Orden values.
    /// </summary>
    private void ReordenarArticulos()
    {
        for (int i = 0; i < Articulos.Count; i++)
            Articulos[i].Orden = i + 1;
    }

    /// <summary>
    /// Recalculates the total from all articles.
    /// </summary>
    public void RecalcularTotal()
    {
        Total = Articulos.Sum(a => a.Total);
    }

    // ===== AUTOCOMPLETE FOR ARTICLE DESCRIPTIONS (data only) =====

    /// <summary>
    /// Loads distinct article descriptions from the repository for autocomplete.
    /// </summary>
    public async Task LoadArticuloSuggestionsAsync()
    {
        var descriptions = await _articleRepository.GetDistinctDescriptionsAsync();
        ArticuloSuggestions = descriptions;
    }

    /// <summary>
    /// Gets filtered article suggestions based on search text.
    /// Code-behind calls this and handles UI (showing/hiding suggestion frames).
    /// </summary>
    public List<string> GetFilteredArticuloSuggestions(string searchText, object? bindingContext = null)
    {
        if (bindingContext is TArticulo articulo)
        {
            _currentArticulo = articulo;
        }

        if (string.IsNullOrWhiteSpace(searchText))
        {
            return new List<string>();
        }

        return ArticuloSuggestions
            .Where(s => s.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// Applies the selected suggestion to the current article.
    /// </summary>
    public void ApplyArticuloSuggestion(string descripcion)
    {
        if (_currentArticulo is not null)
        {
            _currentArticulo.Descripcion = descripcion;
            RecalcularTotal();
        }
    }

    /// <summary>
    /// Notifies that the Articulos collection has changed (for UI binding refresh).
    /// Call this after modifying an article's properties (e.g., ImagenPath).
    /// </summary>
    public void RefreshArticulosBinding()
    {
        OnPropertyChanged(nameof(Articulos));
    }

    // ===== VALIDATION HELPERS =====

    /// <summary>
    /// Validates that there's at least one article.
    /// </summary>
    public bool HasArticulos => Articulos.Count > 0;

    /// <summary>
    /// Validates that all articles have a description.
    /// </summary>
    public bool AllArticulosHaveDescription => Articulos.All(a => !string.IsNullOrWhiteSpace(a.Descripcion));

    /// <summary>
    /// Gets the first article without a description, or null if all have descriptions.
    /// </summary>
    public TArticulo? GetFirstInvalidArticulo() => Articulos.FirstOrDefault(a => string.IsNullOrWhiteSpace(a.Descripcion));

    // ===== INITIALIZATION =====

    /// <summary>
    /// Loads existing articles for an entity (used in edit pages).
    /// </summary>
    public async Task LoadArticulosAsync(int parentId)
    {
        var articulos = await _articleRepository.GetByParentIdAsync(parentId);
        Articulos.Clear();
        foreach (var articulo in articulos)
        {
            Articulos.Add(articulo);
        }
        ReordenarArticulos();
        RecalcularTotal();
        await LoadArticuloSuggestionsAsync();
    }

    /// <summary>
    /// Saves all articles for a parent entity (used in modals and edit pages).
    /// </summary>
    public async Task SaveArticulosAsync(int parentId, bool deleteExisting = false)
    {
        if (deleteExisting)
        {
            var existing = await _articleRepository.GetByParentIdAsync(parentId);
            foreach (var viejo in existing)
            {
                await _articleRepository.DeleteAsync(viejo);
            }
        }

        foreach (var articulo in Articulos)
        {
            articulo.Id = 0; // Reset ID for insert
            SetParentId(articulo, parentId);
            await _articleRepository.SaveAsync(articulo);
        }
    }

    /// <summary>
    /// Sets the parent ID on the article. Must be implemented by derived classes.
    /// </summary>
    protected abstract void SetParentId(TArticulo articulo, int parentId);
}