using Mercader.Domain.Entities;

namespace Mercader.Data.Interfaces;

public interface IArticleRepository<T> where T : ArticuloBase, new()
{
    Task<List<T>> GetByParentIdAsync(int parentId);
    Task<int> SaveAsync(T articulo);
    Task<int> DeleteAsync(T articulo);
    Task<List<string>> GetDistinctDescriptionsAsync();
}