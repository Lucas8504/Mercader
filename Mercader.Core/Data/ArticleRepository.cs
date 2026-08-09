using SQLite;
using Mercader.Data.Interfaces;
using Mercader.Domain.Entities;

namespace Mercader.Data;

public class ArticleRepository<T> : IArticleRepository<T> where T : ArticuloBase, new()
{
    private readonly SQLiteAsyncConnection _connection;
    private readonly string _foreignKeyColumn;
    private readonly string _tableName;

    public ArticleRepository(SQLiteAsyncConnection connection, string foreignKeyColumn)
    {
        _connection = connection;
        _foreignKeyColumn = foreignKeyColumn;
        _tableName = typeof(T).Name switch
        {
            nameof(ArticuloVenta) => "ArticulosVenta",
            nameof(ArticuloGasto) => "ArticulosGasto",
            nameof(ArticuloEncargo) => "ArticulosEncargo",
            _ => typeof(T).Name
        };
    }

    public async Task<List<T>> GetByParentIdAsync(int parentId)
    {
        return await _connection.QueryAsync<T>(
            $@"SELECT * FROM {_tableName} 
               WHERE {_foreignKeyColumn} = ? AND IsDeleted != 1 
               ORDER BY Orden",
            parentId);
    }

    public async Task<int> SaveAsync(T articulo)
    {
        if (articulo.Id != 0)
            return await _connection.UpdateAsync(articulo);
        else
            return await _connection.InsertAsync(articulo);
    }

    public async Task<int> DeleteAsync(T articulo)
    {
        articulo.SoftDelete();
        return await _connection.UpdateAsync(articulo);
    }

    public async Task<List<string>> GetDistinctDescriptionsAsync()
    {
        var articulos = await _connection.Table<T>()
            .Where(a => !a.IsDeleted && !string.IsNullOrEmpty(a.Descripcion))
            .ToListAsync();
        return articulos.Select(a => a.Descripcion!).Distinct().ToList();
    }
}