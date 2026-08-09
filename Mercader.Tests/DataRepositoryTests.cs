using Mercader.Data;
using Mercader.Data.Interfaces;
using Mercader.Domain.Entities;

namespace Mercader.Tests;

/// <summary>
/// Tests para DataRepository con SQLite in-memory.
/// Cada test crea su propia instancia con ":memory:" para aislamiento total.
/// </summary>
public class DataRepositoryTests : IAsyncLifetime
{
    private IDataRepository _repository = null!;
    private IArticleRepository<ArticuloVenta> _articuloVentaRepo = null!;
    private IArticleRepository<ArticuloGasto> _articuloGastoRepo = null!;
    private IArticleRepository<ArticuloEncargo> _articuloEncargoRepo = null!;

    public async Task InitializeAsync()
    {
        _repository = new DataRepository(":memory:");
        await _repository.InitializeDatabaseAsync();
        _articuloVentaRepo = _repository.GetArticleRepository<ArticuloVenta>();
        _articuloGastoRepo = _repository.GetArticleRepository<ArticuloGasto>();
        _articuloEncargoRepo = _repository.GetArticleRepository<ArticuloEncargo>();
    }

    public Task DisposeAsync()
    {
        if (_repository is IAsyncDisposable disposable)
            return disposable.DisposeAsync().AsTask();

        return Task.CompletedTask;
    }

    // ======================================================================
    // Initialize
    // ======================================================================

    [Fact]
    public async Task InitializeDatabaseAsync_CalledTwice_DoesNotThrow()
    {
        await _repository.InitializeDatabaseAsync(); // second call
    }

    [Fact]
    public async Task InitializeDatabaseAsync_TablesExist()
    {
        // GetAllAsync<T> lanza InvalidOperationException si la BD no está iniciada.
        // Si lo llamamos y no tira, las tablas existen.
        var all = await _repository.GetAllAsync<Ventas>();
        Assert.NotNull(all);
    }

    // ======================================================================
    // Ventas CRUD
    // ======================================================================

    [Fact]
    public async Task SaveVenta_Insert_ReturnsPositiveId()
    {
        var venta = CreateSampleVenta();

        var id = await _repository.SaveVentasAsync(venta);

        Assert.True(id > 0);
        Assert.True(venta.Id > 0);
    }

    [Fact]
    public async Task SaveVenta_Insert_SetsCreatedAt()
    {
        var venta = CreateSampleVenta();

        await _repository.SaveVentasAsync(venta);

        Assert.NotEqual(default, venta.CreatedAt);
    }

    [Fact]
    public async Task SaveVenta_Update_ReturnsModifiedCount()
    {
        var venta = CreateSampleVenta();
        await _repository.SaveVentasAsync(venta);

        venta.Descripcion = "Modificada";
        var count = await _repository.SaveVentasAsync(venta);

        Assert.Equal(1, count); // UpdateAsync returns number of rows modified
    }

    [Fact]
    public async Task SaveVenta_Update_SetsUpdatedAt()
    {
        var venta = CreateSampleVenta();
        await _repository.SaveVentasAsync(venta);

        Assert.Null(venta.UpdatedAt);

        venta.Descripcion = "Modificada";
        await _repository.SaveVentasAsync(venta);

        Assert.NotNull(venta.UpdatedAt);
    }

    [Fact]
    public async Task GetVentas_ReturnsInsertedVentas()
    {
        await _repository.SaveVentasAsync(CreateSampleVenta());
        await _repository.SaveVentasAsync(CreateSampleVenta("Venta 2"));

        var ventas = await _repository.GetVentasAsync();

        Assert.Equal(2, ventas.Count);
    }

    [Fact]
    public async Task GetVentas_ExcludesSoftDeleted()
    {
        var venta = CreateSampleVenta();
        await _repository.SaveVentasAsync(venta);
        await _repository.SaveVentasAsync(CreateSampleVenta("Venta 2"));

        await _repository.DeleteVentaAsync(venta);

        var ventas = await _repository.GetVentasAsync();
        Assert.Single(ventas);
    }

    // ======================================================================
    // Gastos CRUD
    // ======================================================================

    [Fact]
    public async Task SaveGasto_Insert_ReturnsPositiveId()
    {
        var gasto = CreateSampleGasto();

        var id = await _repository.SaveGastoAsync(gasto);

        Assert.True(id > 0);
    }

    [Fact]
    public async Task SaveGasto_Insert_SetsCreatedAt()
    {
        var gasto = CreateSampleGasto();

        await _repository.SaveGastoAsync(gasto);

        Assert.NotEqual(default, gasto.CreatedAt);
    }

    [Fact]
    public async Task GetGastos_ReturnsInsertedGastos()
    {
        await _repository.SaveGastoAsync(CreateSampleGasto());
        await _repository.SaveGastoAsync(CreateSampleGasto(monto: 200));

        var gastos = await _repository.GetGastosAsync();

        Assert.Equal(2, gastos.Count);
    }

    [Fact]
    public async Task DeleteGasto_SoftDelete_ExcludesFromGetGastos()
    {
        var gasto = CreateSampleGasto();
        await _repository.SaveGastoAsync(gasto);

        await _repository.DeleteGastoAsync(gasto);

        var gastos = await _repository.GetGastosAsync();
        Assert.Empty(gastos);
    }

    // ======================================================================
    // Encargos CRUD
    // ======================================================================

    [Fact]
    public async Task SaveEncargo_Insert_ReturnsPositiveId()
    {
        var encargo = CreateSampleEncargo();

        var id = await _repository.SaveEncargoAsync(encargo);

        Assert.True(id > 0);
    }

    [Fact]
    public async Task GetEncargos_ReturnsInsertedEncargos()
    {
        await _repository.SaveEncargoAsync(CreateSampleEncargo());
        await _repository.SaveEncargoAsync(CreateSampleEncargo(nombre: "Otro"));

        var encargos = await _repository.GetEncargosAsync();

        Assert.Equal(2, encargos.Count);
    }

    [Fact]
    public async Task DeleteEncargo_SoftDelete_ExcludesFromGetEncargos()
    {
        var encargo = CreateSampleEncargo();
        await _repository.SaveEncargoAsync(encargo);

        await _repository.DeleteEncargoAsync(encargo);

        var encargos = await _repository.GetEncargosAsync();
        Assert.Empty(encargos);
    }

    // ======================================================================
    // Soft delete: GetAllAsync / GetDeletedAsync
    // ======================================================================

    [Fact]
    public async Task GetAllAsync_IncludesSoftDeleted()
    {
        var venta = CreateSampleVenta();
        await _repository.SaveVentasAsync(venta);
        await _repository.DeleteVentaAsync(venta);

        var all = await _repository.GetAllAsync<Ventas>();

        Assert.Single(all);
        Assert.True(all[0].IsDeleted);
    }

    [Fact]
    public async Task GetDeletedAsync_ReturnsOnlySoftDeleted()
    {
        await _repository.SaveVentasAsync(CreateSampleVenta());
        var venta2 = CreateSampleVenta("To delete");
        await _repository.SaveVentasAsync(venta2);
        await _repository.DeleteVentaAsync(venta2);

        var deleted = await _repository.GetDeletedAsync<Ventas>();

        Assert.Single(deleted);
        Assert.Equal("To delete", deleted[0].Descripcion);
    }

    [Fact]
    public async Task GetDeletedAsync_Empty_WhenNoSoftDeletes()
    {
        await _repository.SaveVentasAsync(CreateSampleVenta());

        var deleted = await _repository.GetDeletedAsync<Ventas>();

        Assert.Empty(deleted);
    }

    // ======================================================================
    // Autocomplete: Dismiss / Reinstate
    // ======================================================================

    [Fact]
    public async Task DismissAutocomplete_InsertsDescripcionOculta()
    {
        // Insert a venta, dismiss its description, then it shouldn't appear in distinct
        var venta = CreateSampleVenta(descripcion: "Cosita");
        await _repository.SaveVentasAsync(venta);

        await _repository.DismissAutocompleteDescriptionAsync("Cosita", "Venta");

        var descs = await _repository.GetDistinctVentasDescriptionsAsync();
        Assert.DoesNotContain("Cosita", descs);
    }

    [Fact]
    public async Task DismissAutocomplete_Idempotent_DoesNotThrow()
    {
        await _repository.DismissAutocompleteDescriptionAsync("test", "Venta");
        await _repository.DismissAutocompleteDescriptionAsync("test", "Venta");
        // No exception = pass
    }

    [Fact]
    public async Task DismissAutocomplete_EmptyString_DoesNothing()
    {
        // Should return early without throwing
        await _repository.DismissAutocompleteDescriptionAsync("", "Venta");
    }

    [Fact]
    public async Task ReinstateAutocomplete_RemovesDescripcionOculta()
    {
        var venta = CreateSampleVenta(descripcion: "Reinstable");
        await _repository.SaveVentasAsync(venta);
        await _repository.DismissAutocompleteDescriptionAsync("Reinstable", "Venta");

        await _repository.ReinstateAutocompleteDescriptionAsync("Reinstable", "Venta");

        var descs = await _repository.GetDistinctVentasDescriptionsAsync();
        Assert.Contains("Reinstable", descs);
    }

    [Fact]
    public async Task ReinstateAutocomplete_NonExistent_DoesNotThrow()
    {
        await _repository.ReinstateAutocompleteDescriptionAsync("no-existe", "Venta");
    }

    [Fact]
    public async Task DistinctDescriptions_RespectsEntityType()
    {
        var venta = CreateSampleVenta(descripcion: "Compartida");
        await _repository.SaveVentasAsync(venta);

        // Dismiss for Gasto only — Venta should still show it
        await _repository.DismissAutocompleteDescriptionAsync("Compartida", "Gasto");

        var ventaDescs = await _repository.GetDistinctVentasDescriptionsAsync();
        Assert.Contains("Compartida", ventaDescs);

        // Now dismiss for Venta too
        await _repository.DismissAutocompleteDescriptionAsync("Compartida", "Venta");
        ventaDescs = await _repository.GetDistinctVentasDescriptionsAsync();
        Assert.DoesNotContain("Compartida", ventaDescs);
    }

    // ======================================================================
    // Date-range queries
    // ======================================================================

    [Fact]
    public async Task GetVentasUltimos6Meses_RespectsDateRange()
    {
        var oldVenta = CreateSampleVenta();
        oldVenta.Fecha = DateTime.Now.AddMonths(-7);
        await _repository.SaveVentasAsync(oldVenta);

        var recentVenta = CreateSampleVenta("Reciente");
        recentVenta.Fecha = DateTime.Now.AddMonths(-1);
        await _repository.SaveVentasAsync(recentVenta);

        var result = await _repository.GetVentasUltimos6MesesAsync();

        Assert.Single(result);
        Assert.Equal("Reciente", result[0].Descripcion);
    }

    [Fact]
    public async Task GetGastosUltimos6Meses_RespectsDateRange()
    {
        var oldGasto = CreateSampleGasto();
        oldGasto.Fecha = DateTime.Now.AddMonths(-8);
        await _repository.SaveGastoAsync(oldGasto);

        var recentGasto = CreateSampleGasto(monto: 500);
        recentGasto.Fecha = DateTime.Now.AddMonths(-2);
        await _repository.SaveGastoAsync(recentGasto);

        var result = await _repository.GetGastosUltimos6MesesAsync();

        Assert.Single(result);
    }

    [Fact]
    public async Task GetVentasUltimos6Meses_ExcludesSoftDeleted()
    {
        var venta = CreateSampleVenta("To delete");
        venta.Fecha = DateTime.Now.AddMonths(-1);
        await _repository.SaveVentasAsync(venta);
        await _repository.DeleteVentaAsync(venta);

        var result = await _repository.GetVentasUltimos6MesesAsync();

        Assert.Empty(result);
    }

    // ======================================================================
    // Argument validation
    // ======================================================================

    [Fact]
    public async Task SaveVenta_NullArgument_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _repository.SaveVentasAsync(null!));
    }

    [Fact]
    public async Task DeleteVenta_NullArgument_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _repository.DeleteVentaAsync(null!));
    }

    // ======================================================================
    // 6.1 Article CRUD tests
    // ======================================================================

    [Fact]
    public async Task SaveArticuloVenta_Insert_ReturnsPositiveId()
    {
        var venta = CreateSampleVenta();
        await _repository.SaveVentasAsync(venta);

        var articulo = new ArticuloVenta { VentaId = venta.Id, Descripcion = "Art 1", PrecioUnitario = 50, Cantidad = 2, Orden = 1 };
        var id = await _articuloVentaRepo.SaveAsync(articulo);

        Assert.True(id > 0);
    }

    [Fact]
    public async Task GetArticulosVenta_ReturnsArticlesForParent()
    {
        var venta = CreateSampleVenta();
        await _repository.SaveVentasAsync(venta);

        await _articuloVentaRepo.SaveAsync(new ArticuloVenta { VentaId = venta.Id, Descripcion = "A1", PrecioUnitario = 10, Cantidad = 1, Orden = 1 });
        await _articuloVentaRepo.SaveAsync(new ArticuloVenta { VentaId = venta.Id, Descripcion = "A2", PrecioUnitario = 20, Cantidad = 2, Orden = 2 });

        var articulos = await _articuloVentaRepo.GetByParentIdAsync(venta.Id);

        Assert.Equal(2, articulos.Count);
        Assert.Contains(articulos, a => a.Descripcion == "A1" && a.Total == 10);
        Assert.Contains(articulos, a => a.Descripcion == "A2" && a.Total == 40);
    }

    [Fact]
    public async Task GetArticulosVenta_OrderedByOrden()
    {
        var venta = CreateSampleVenta();
        await _repository.SaveVentasAsync(venta);

        await _articuloVentaRepo.SaveAsync(new ArticuloVenta { VentaId = venta.Id, Descripcion = "B", PrecioUnitario = 10, Cantidad = 1, Orden = 2 });
        await _articuloVentaRepo.SaveAsync(new ArticuloVenta { VentaId = venta.Id, Descripcion = "A", PrecioUnitario = 10, Cantidad = 1, Orden = 1 });

        var articulos = await _articuloVentaRepo.GetByParentIdAsync(venta.Id);

        Assert.Equal(2, articulos.Count);
        Assert.Equal("A", articulos[0].Descripcion);
        Assert.Equal("B", articulos[1].Descripcion);
    }

    [Fact]
    public async Task GetArticulosVenta_ReturnsEmptyForNonExistentParent()
    {
        var articulos = await _articuloVentaRepo.GetByParentIdAsync(999);
        Assert.Empty(articulos);
    }

    [Fact]
    public async Task DeleteArticuloVenta_SoftDelete_ExcludesFromGet()
    {
        var venta = CreateSampleVenta();
        await _repository.SaveVentasAsync(venta);

        var articulo = new ArticuloVenta { VentaId = venta.Id, Descripcion = "Del", PrecioUnitario = 10, Cantidad = 1, Orden = 1 };
        await _articuloVentaRepo.SaveAsync(articulo);

        await _articuloVentaRepo.DeleteAsync(articulo);

        var articulos = await _articuloVentaRepo.GetByParentIdAsync(venta.Id);
        Assert.Empty(articulos);
    }

    [Fact]
    public async Task SaveArticuloGasto_Insert_ReturnsPositiveId()
    {
        var gasto = CreateSampleGasto();
        await _repository.SaveGastoAsync(gasto);

        var articulo = new ArticuloGasto { GastoId = gasto.Id, Descripcion = "Art G", PrecioUnitario = 30, Cantidad = 3, Orden = 1 };
        var id = await _articuloGastoRepo.SaveAsync(articulo);

        Assert.True(id > 0);
    }

    [Fact]
    public async Task GetArticulosGasto_ReturnsArticlesForParent()
    {
        var gasto = CreateSampleGasto();
        await _repository.SaveGastoAsync(gasto);

        await _articuloGastoRepo.SaveAsync(new ArticuloGasto { GastoId = gasto.Id, Descripcion = "G1", PrecioUnitario = 15, Cantidad = 2, Orden = 1 });

        var articulos = await _articuloGastoRepo.GetByParentIdAsync(gasto.Id);
        Assert.Single(articulos);
        Assert.Equal(30, articulos[0].Total);
    }

    [Fact]
    public async Task SaveArticuloEncargo_Insert_ReturnsPositiveId()
    {
        var encargo = CreateSampleEncargo();
        await _repository.SaveEncargoAsync(encargo);

        var articulo = new ArticuloEncargo { EncargoId = encargo.Id, Descripcion = "Art E", PrecioUnitario = 40, Cantidad = 1, Orden = 1 };
        var id = await _articuloEncargoRepo.SaveAsync(articulo);

        Assert.True(id > 0);
    }

    [Fact]
    public async Task GetArticulosEncargo_ReturnsArticlesForParent()
    {
        var encargo = CreateSampleEncargo();
        await _repository.SaveEncargoAsync(encargo);

        await _articuloEncargoRepo.SaveAsync(new ArticuloEncargo { EncargoId = encargo.Id, Descripcion = "E1", PrecioUnitario = 25, Cantidad = 4, Orden = 1 });

        var articulos = await _articuloEncargoRepo.GetByParentIdAsync(encargo.Id);
        Assert.Single(articulos);
        Assert.Equal(100, articulos[0].Total);
    }

    // ======================================================================
    // 6.2 Delete-last-article tests
    // ======================================================================

    [Fact]
    public async Task DeleteLastArticulo_EntityStillExists_NoCascade()
    {
        // El repositorio NO elimina automáticamente el padre al borrar el último artículo
        var venta = CreateSampleVenta();
        await _repository.SaveVentasAsync(venta);

        var articulo = new ArticuloVenta { VentaId = venta.Id, Descripcion = "Unico", PrecioUnitario = 10, Cantidad = 1, Orden = 1 };
        await _articuloVentaRepo.SaveAsync(articulo);
        await _articuloVentaRepo.DeleteAsync(articulo);

        // El padre debe seguir existiendo (la cascada es responsabilidad del VM/UI)
        var ventas = await _repository.GetVentasAsync();
        Assert.Contains(ventas, v => v.Id == venta.Id);
    }

    [Fact]
    public async Task DeleteLastArticulo_AndManuallyDeleteParent_OrphansCleaned()
    {
        // Simula la lógica del VM: borrar último artículo → borrar padre
        var venta = CreateSampleVenta();
        await _repository.SaveVentasAsync(venta);

        var articulo = new ArticuloVenta { VentaId = venta.Id, Descripcion = "Unico", PrecioUnitario = 10, Cantidad = 1, Orden = 1 };
        await _articuloVentaRepo.SaveAsync(articulo);
        await _articuloVentaRepo.DeleteAsync(articulo);

        // Ahora borrar el padre manualmente
        await _repository.DeleteVentaAsync(venta);

        var ventas = await _repository.GetVentasAsync();
        Assert.DoesNotContain(ventas, v => v.Id == venta.Id);
    }

    // ======================================================================
    // 6.4 Encargo → Venta conversion tests
    // ======================================================================

    [Fact]
    public async Task ConvertEncargoToVenta_TransfersArticles()
    {
        // Crear encargo con 2 artículos
        var encargo = CreateSampleEncargo(nombre: "Convert");
        await _repository.SaveEncargoAsync(encargo);

        var art1 = new ArticuloEncargo { EncargoId = encargo.Id, Descripcion = "Prod A", PrecioUnitario = 100, Cantidad = 2, Orden = 1 };
        var art2 = new ArticuloEncargo { EncargoId = encargo.Id, Descripcion = "Prod B", PrecioUnitario = 50, Cantidad = 3, Orden = 2 };
        await _articuloEncargoRepo.SaveAsync(art1);
        await _articuloEncargoRepo.SaveAsync(art2);

        // Crear venta con total de los artículos del encargo
        var articulosEncargo = await _articuloEncargoRepo.GetByParentIdAsync(encargo.Id);
        var totalEncargo = articulosEncargo.Sum(a => a.Total);

        var venta = new Ventas
        {
            Descripcion = $"Venta de: {encargo.Nombre} - {encargo.Descripcion}",
            Precio = totalEncargo,
            Cantidad = 1,
            Fecha = DateTime.Now
        };
        await _repository.SaveVentasAsync(venta);

        // Transferir artículos
        foreach (var ae in articulosEncargo)
        {
            await _articuloVentaRepo.SaveAsync(new ArticuloVenta
            {
                VentaId = venta.Id,
                Descripcion = ae.Descripcion,
                PrecioUnitario = ae.PrecioUnitario,
                Cantidad = ae.Cantidad,
                Orden = ae.Orden
            });
        }

        // Verificar que la venta tiene los artículos transferidos
        var articulosVenta = await _articuloVentaRepo.GetByParentIdAsync(venta.Id);
        Assert.Equal(2, articulosVenta.Count);
        Assert.Contains(articulosVenta, a => a.Descripcion == "Prod A" && a.PrecioUnitario == 100 && a.Cantidad == 2);
        Assert.Contains(articulosVenta, a => a.Descripcion == "Prod B" && a.PrecioUnitario == 50 && a.Cantidad == 3);
        Assert.Equal(350, articulosVenta.Sum(a => a.Total)); // 100*2 + 50*3
    }

    [Fact]
    public async Task ConvertEncargoToVenta_ArticlesPreserveOrder()
    {
        var encargo = CreateSampleEncargo(nombre: "Order");
        await _repository.SaveEncargoAsync(encargo);

        await _articuloEncargoRepo.SaveAsync(new ArticuloEncargo { EncargoId = encargo.Id, Descripcion = "Z", PrecioUnitario = 10, Cantidad = 1, Orden = 2 });
        await _articuloEncargoRepo.SaveAsync(new ArticuloEncargo { EncargoId = encargo.Id, Descripcion = "A", PrecioUnitario = 10, Cantidad = 1, Orden = 1 });

        var articulosEncargo = await _articuloEncargoRepo.GetByParentIdAsync(encargo.Id);

        var venta = new Ventas { Descripcion = "Venta order", Precio = articulosEncargo.Sum(a => a.Total), Cantidad = 1, Fecha = DateTime.Now };
        await _repository.SaveVentasAsync(venta);

        foreach (var ae in articulosEncargo)
        {
            await _articuloVentaRepo.SaveAsync(new ArticuloVenta
            {
                VentaId = venta.Id,
                Descripcion = ae.Descripcion,
                PrecioUnitario = ae.PrecioUnitario,
                Cantidad = ae.Cantidad,
                Orden = ae.Orden
            });
        }

        var articulosVenta = await _articuloVentaRepo.GetByParentIdAsync(venta.Id);
        Assert.Equal(2, articulosVenta.Count);
        Assert.Equal("A", articulosVenta[0].Descripcion); // Orden 1
        Assert.Equal("Z", articulosVenta[1].Descripcion); // Orden 2
    }

    [Fact]
    public async Task ConvertEncargoToVenta_OriginalArticlesRetainedInEncargo()
    {
        var encargo = CreateSampleEncargo(nombre: "Retain");
        await _repository.SaveEncargoAsync(encargo);

        await _articuloEncargoRepo.SaveAsync(new ArticuloEncargo { EncargoId = encargo.Id, Descripcion = "Keep", PrecioUnitario = 10, Cantidad = 1, Orden = 1 });

        // Transferir (simular conversión)
        var articulosEncargo = await _articuloEncargoRepo.GetByParentIdAsync(encargo.Id);
        var venta = new Ventas { Descripcion = "Venta retain", Precio = articulosEncargo.Sum(a => a.Total), Cantidad = 1, Fecha = DateTime.Now };
        await _repository.SaveVentasAsync(venta);

        foreach (var ae in articulosEncargo)
        {
            await _articuloVentaRepo.SaveAsync(new ArticuloVenta
            {
                VentaId = venta.Id,
                Descripcion = ae.Descripcion,
                PrecioUnitario = ae.PrecioUnitario,
                Cantidad = ae.Cantidad,
                Orden = ae.Orden
            });
        }

        // Los artículos originales del encargo aún existen (no se borraron)
        var originales = await _articuloEncargoRepo.GetByParentIdAsync(encargo.Id);
        Assert.Single(originales);
        Assert.Equal("Keep", originales[0].Descripcion);
    }

    // ======================================================================
    // Sample data factories
    // ======================================================================

    private static Ventas CreateSampleVenta(string? descripcion = "Venta test", decimal precio = 100)
    {
        return new Ventas
        {
            Descripcion = descripcion,
            Precio = precio,
            Cantidad = 2,
            Fecha = DateTime.Now
        };
    }

    private static Gasto CreateSampleGasto(decimal monto = 50)
    {
        return new Gasto
        {
            Descripcion = "Gasto test",
            Monto = monto,
            Cantidad = 1,
            Fecha = DateTime.Now
        };
    }

    private static Encargo CreateSampleEncargo(string nombre = "Cliente test")
    {
        return new Encargo
        {
            Nombre = nombre,
            Contacto = "test@mail.com",
            Precio = 200,
            Cantidad = 1,
            Descripcion = "Encargo test",
            Fecha = DateTime.Now,
            FechaEntrega = DateTime.Now.AddDays(30)
        };
    }
}
