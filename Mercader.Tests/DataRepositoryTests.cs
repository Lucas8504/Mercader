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

    public async Task InitializeAsync()
    {
        _repository = new DataRepository(":memory:");
        await _repository.InitializeDatabaseAsync();
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
