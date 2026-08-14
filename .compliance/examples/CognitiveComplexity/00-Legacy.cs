namespace ComplianceExamples.CognitiveComplexity;

public sealed class LegacyOrderApproval
{
    public string Approve(Order order, User user, DateTimeOffset now)
    {
        if (order is not null)
        {
            if (user is not null)
            {
                if (user.IsActive)
                {
                    if (order.Lines.Count > 0)
                    {
                        if (order.Total > 0)
                        {
                            if (order.Total > user.ApprovalLimit)
                            {
                                if (!user.CanEscalate)
                                {
                                    return "limit-exceeded";
                                }

                                return "manual-review";
                            }

                            if (order.RequestedDelivery < now)
                            {
                                return "delivery-in-past";
                            }

                            return "approved";
                        }

                        return "invalid-total";
                    }

                    return "empty-order";
                }

                return "inactive-user";
            }

            return "missing-user";
        }

        return "missing-order";
    }
}

public sealed record Order(decimal Total, DateTimeOffset RequestedDelivery, IReadOnlyList<string> Lines);

public sealed record User(bool IsActive, decimal ApprovalLimit, bool CanEscalate);
