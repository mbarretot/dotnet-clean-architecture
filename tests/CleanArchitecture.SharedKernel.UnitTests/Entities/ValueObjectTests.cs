using CleanArchitecture.SharedKernel.Entities;
using Shouldly;

namespace CleanArchitecture.SharedKernel.UnitTests.Entities;

public class ValueObjectTests
{
    private sealed class Money(decimal amount, string currency) : ValueObject
    {
        public decimal Amount { get; } = amount;

        public string Currency { get; } = currency;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }
    }

    [Fact]
    public void TwoValueObjects_WithSameComponents_AreEqual()
    {
        var first = new Money(10, "USD");
        var second = new Money(10, "USD");

        first.ShouldBe(second);
        (first == second).ShouldBeTrue();
    }

    [Fact]
    public void TwoValueObjects_WithDifferentComponents_AreNotEqual()
    {
        var first = new Money(10, "USD");
        var second = new Money(20, "USD");

        first.ShouldNotBe(second);
        (first != second).ShouldBeTrue();
    }

    [Fact]
    public void GetHashCode_ForEqualValueObjects_IsConsistent()
    {
        var first = new Money(10, "USD");
        var second = new Money(10, "USD");

        first.GetHashCode().ShouldBe(second.GetHashCode());
    }
}
