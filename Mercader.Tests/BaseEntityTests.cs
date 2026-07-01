using Mercader.Domain.Entities;

namespace Mercader.Tests;

public class BaseEntityTests
{
    // ======================================================================
    // Default values
    // ======================================================================

    [Fact]
    public void NewEntity_Id_DefaultsToZero()
    {
        var entity = new TestEntity();

        Assert.Equal(0, entity.Id);
    }

    [Fact]
    public void NewEntity_IsDeleted_DefaultsToFalse()
    {
        var entity = new TestEntity();

        Assert.False(entity.IsDeleted);
    }

    [Fact]
    public void NewEntity_CreatedAt_IsRecentUtc()
    {
        var entity = new TestEntity();
        var now = DateTime.UtcNow;

        Assert.True(entity.CreatedAt <= now);
        Assert.True(entity.CreatedAt > now.AddMinutes(-1));
    }

    [Fact]
    public void NewEntity_UpdatedAt_DefaultsToNull()
    {
        var entity = new TestEntity();

        Assert.Null(entity.UpdatedAt);
    }

    // ======================================================================
    // SoftDelete
    // ======================================================================

    [Fact]
    public void SoftDelete_SetsIsDeletedTrue()
    {
        var entity = new TestEntity();
        Assert.False(entity.IsDeleted);

        entity.SoftDelete();

        Assert.True(entity.IsDeleted);
    }

    [Fact]
    public void SoftDelete_SetsUpdatedAt()
    {
        var entity = new TestEntity();
        Assert.Null(entity.UpdatedAt);

        entity.SoftDelete();

        Assert.NotNull(entity.UpdatedAt);
    }

    [Fact]
    public void SoftDelete_UpdatedAt_IsRecentUtc()
    {
        var entity = new TestEntity();
        entity.SoftDelete();
        var now = DateTime.UtcNow;

        Assert.True(entity.UpdatedAt <= now);
        Assert.True(entity.UpdatedAt > now.AddMinutes(-1));
    }

    [Fact]
    public void SoftDelete_DoesNotChangeCreatedAt()
    {
        var entity = new TestEntity();
        var originalCreatedAt = entity.CreatedAt;

        Thread.Sleep(10); // ensure time difference if granularity matters
        entity.SoftDelete();

        Assert.Equal(originalCreatedAt, entity.CreatedAt);
    }

    [Fact]
    public void SoftDelete_Idempotent_CanCallTwice()
    {
        var entity = new TestEntity();

        entity.SoftDelete();
        entity.SoftDelete();

        Assert.True(entity.IsDeleted);
        Assert.NotNull(entity.UpdatedAt);
    }

    // ======================================================================
    // MarkUpdated
    // ======================================================================

    [Fact]
    public void MarkUpdated_SetsUpdatedAt()
    {
        var entity = new TestEntity();
        Assert.Null(entity.UpdatedAt);

        entity.MarkUpdated();

        Assert.NotNull(entity.UpdatedAt);
    }

    [Fact]
    public void MarkUpdated_UpdatedAt_IsRecentUtc()
    {
        var entity = new TestEntity();
        entity.MarkUpdated();
        var now = DateTime.UtcNow;

        Assert.True(entity.UpdatedAt <= now);
        Assert.True(entity.UpdatedAt > now.AddMinutes(-1));
    }

    [Fact]
    public void MarkUpdated_DoesNotChangeCreatedAt()
    {
        var entity = new TestEntity();
        var originalCreatedAt = entity.CreatedAt;

        entity.MarkUpdated();

        Assert.Equal(originalCreatedAt, entity.CreatedAt);
    }

    [Fact]
    public void MarkUpdated_DoesNotMarkAsDeleted()
    {
        var entity = new TestEntity();

        entity.MarkUpdated();

        Assert.False(entity.IsDeleted);
    }

    // ======================================================================
    // Concrete entity: Encargo
    // ======================================================================

    [Fact]
    public void Encargo_InheritsBaseDefaults()
    {
        var encargo = new Encargo();

        Assert.Equal(0, encargo.Id);
        Assert.False(encargo.IsDeleted);
        Assert.Null(encargo.UpdatedAt);
    }

    // ======================================================================
    // Helper: test entity to avoid using a concrete one from Domain
    // ======================================================================

    private sealed class TestEntity : BaseEntity
    {
    }
}
