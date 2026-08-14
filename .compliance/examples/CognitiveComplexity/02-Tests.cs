using Xunit;

namespace ComplianceExamples.CognitiveComplexity;

public sealed class OrderApprovalPolicyTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(false, 100, false, 50, "inactive-user")]
    [InlineData(true, 100, false, 101, "limit-exceeded")]
    [InlineData(true, 100, true, 101, "manual-review")]
    [InlineData(true, 100, false, 100, "approved")]
    public void Decide_preserves_each_business_branch(
        bool active,
        decimal limit,
        bool canEscalate,
        decimal total,
        string expectedCode)
    {
        var order = new Order(total, Now.AddDays(1), ["line"]);
        var user = new User(active, limit, canEscalate);

        var result = new OrderApprovalPolicy().Decide(order, user, Now);

        Assert.Equal(expectedCode, result.Code);
    }

    [Fact]
    public void Decide_rejects_delivery_in_the_past()
    {
        var order = new Order(50, Now.AddTicks(-1), ["line"]);
        var user = new User(true, 100, false);

        var result = new OrderApprovalPolicy().Decide(order, user, Now);

        Assert.Equal(ApprovalStatus.Rejected, result.Status);
        Assert.Equal("delivery-in-past", result.Code);
    }

    [Fact]
    public void Decide_preserves_high_value_review_precedence_over_delivery_check()
    {
        var order = new Order(101, Now.AddDays(-1), ["line"]);
        var user = new User(true, 100, true);

        var result = new OrderApprovalPolicy().Decide(order, user, Now);

        Assert.Equal("manual-review", result.Code);
    }
}
