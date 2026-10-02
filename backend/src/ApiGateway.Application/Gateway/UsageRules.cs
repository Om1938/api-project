using System.Linq.Expressions;
using ApiGateway.Domain.Entities;
using ApiGateway.Domain.Enums;

namespace ApiGateway.Application.Gateway;

public static class UsageRules
{
    public static readonly Expression<Func<UsageRecord, bool>> ConsumesQuota =
        r => r.Outcome == RequestOutcome.Success || r.Outcome == RequestOutcome.Failed;
}
