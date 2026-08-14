namespace ComplianceExamples.CognitiveComplexity;

public sealed class OrderApprovalPolicy
{
    public ApprovalDecision Decide(Order? order, User? user, DateTimeOffset now)
    {
        if (order is null)
        {
            return ApprovalDecision.Reject("missing-order");
        }

        if (user is null)
        {
            return ApprovalDecision.Reject("missing-user");
        }

        var invalid = ValidateBase(order, user);
        if (invalid is not null)
        {
            return invalid;
        }

        if (order.Total > user.ApprovalLimit)
        {
            return user.CanEscalate
                ? ApprovalDecision.Review("manual-review")
                : ApprovalDecision.Reject("limit-exceeded");
        }

        return order.RequestedDelivery < now
            ? ApprovalDecision.Reject("delivery-in-past")
            : ApprovalDecision.Approve();
    }

    private static ApprovalDecision? ValidateBase(Order order, User user)
    {
        if (!user.IsActive)
        {
            return ApprovalDecision.Reject("inactive-user");
        }

        if (order.Lines.Count == 0)
        {
            return ApprovalDecision.Reject("empty-order");
        }

        if (order.Total <= 0)
        {
            return ApprovalDecision.Reject("invalid-total");
        }

        return null;
    }
}

public enum ApprovalStatus
{
    Approved,
    Rejected,
    ManualReview,
}

public sealed record ApprovalDecision(ApprovalStatus Status, string Code)
{
    public static ApprovalDecision Approve() => new(ApprovalStatus.Approved, "approved");

    public static ApprovalDecision Reject(string code) => new(ApprovalStatus.Rejected, code);

    public static ApprovalDecision Review(string code) => new(ApprovalStatus.ManualReview, code);
}
