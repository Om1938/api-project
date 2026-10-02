using ApiGateway.Domain.Enums;

namespace ApiGateway.Domain.Entities;

public sealed class CreditTransaction : Entity
{
    private CreditTransaction() { }

    public CreditTransaction(Guid consumerId, decimal amount, CreditTransactionType type, string description)
    {
        ConsumerId = consumerId;
        Amount = amount;
        Type = type;
        Description = description;
    }

    public Guid ConsumerId { get; private set; }
    public decimal Amount { get; private set; }
    public CreditTransactionType Type { get; private set; }
    public string Description { get; private set; } = null!;
}
