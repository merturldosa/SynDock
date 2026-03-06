using FluentAssertions;
using Shop.Application.Common;

namespace Shop.Tests.Unit.Services;

public class PlanLimitsTests
{
    [Theory]
    [InlineData("Free", 20)]
    [InlineData("Basic", 200)]
    [InlineData("Pro", 2000)]
    [InlineData("Enterprise", int.MaxValue)]
    public void GetLimits_ReturnsCorrectMaxProducts(string plan, int expectedMax)
    {
        var limits = PlanLimits.GetLimits(plan);
        limits.MaxProducts.Should().Be(expectedMax);
    }

    [Theory]
    [InlineData("Free", 100)]
    [InlineData("Basic", 1000)]
    [InlineData("Pro", 10000)]
    public void GetLimits_ReturnsCorrectMaxMonthlyOrders(string plan, int expectedMax)
    {
        var limits = PlanLimits.GetLimits(plan);
        limits.MaxMonthlyOrders.Should().Be(expectedMax);
    }

    [Fact]
    public void GetLimits_UnknownPlan_ReturnsFreeDefaults()
    {
        var limits = PlanLimits.GetLimits("NonExistent");
        limits.Should().Be(PlanLimits.GetLimits("Free"));
    }

    [Fact]
    public void GetLimits_CaseInsensitive()
    {
        var lower = PlanLimits.GetLimits("free");
        var upper = PlanLimits.GetLimits("FREE");
        var mixed = PlanLimits.GetLimits("Free");

        lower.Should().Be(upper);
        upper.Should().Be(mixed);
    }

    [Theory]
    [InlineData("Free", 0)]
    [InlineData("Basic", 29000)]
    [InlineData("Pro", 79000)]
    [InlineData("Enterprise", 199000)]
    public void GetPrice_ReturnsCorrectValues(string plan, decimal expectedPrice)
    {
        PlanLimits.GetPrice(plan).Should().Be(expectedPrice);
    }

    [Fact]
    public void GetPrice_UnknownPlan_ReturnsZero()
    {
        PlanLimits.GetPrice("Platinum").Should().Be(0m);
    }

    [Fact]
    public void GetAllPlans_ReturnsFourPlans()
    {
        var plans = PlanLimits.GetAllPlans();
        plans.Should().HaveCount(4);
        plans.Select(p => p.PlanType).Should().Contain(new[] { "Free", "Basic", "Pro", "Enterprise" });
    }
}
