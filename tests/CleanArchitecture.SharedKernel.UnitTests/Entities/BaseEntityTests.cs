using CleanArchitecture.SharedKernel.Entities;
using Shouldly;

namespace CleanArchitecture.SharedKernel.UnitTests.Entities;

public class BaseEntityTests
{
    private sealed class TestEntity(Guid id) : BaseEntity(id);

    private sealed class OtherTestEntity(Guid id) : BaseEntity(id);

    [Fact]
    public void TwoEntities_WithSameIdAndType_AreEqual()
    {
        var id = Guid.NewGuid();
        var first = new TestEntity(id);
        var second = new TestEntity(id);

        first.ShouldBe(second);
        (first == second).ShouldBeTrue();
    }

    [Fact]
    public void TwoEntities_WithDifferentIds_AreNotEqual()
    {
        var first = new TestEntity(Guid.NewGuid());
        var second = new TestEntity(Guid.NewGuid());

        first.ShouldNotBe(second);
    }

    [Fact]
    public void TwoEntities_WithSameIdButDifferentType_AreNotEqual()
    {
        var id = Guid.NewGuid();
        var first = new TestEntity(id);
        var second = new OtherTestEntity(id);

        first.Equals(second).ShouldBeFalse();
    }

    [Fact]
    public void GetHashCode_ForSameIdAndType_IsConsistent()
    {
        var id = Guid.NewGuid();
        var first = new TestEntity(id);
        var second = new TestEntity(id);

        first.GetHashCode().ShouldBe(second.GetHashCode());
    }

    [Fact]
    public void Entity_ExposesAuditableProperties()
    {
        var entity = new TestEntity(Guid.NewGuid());

        entity.CreatedBy.ShouldBe(string.Empty);
        entity.ModifiedBy.ShouldBeNull();
        entity.ModifiedAt.ShouldBeNull();
    }
}
