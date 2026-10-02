namespace ApiGateway.Domain.Entities;

public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.CreateVersion7();

    public DateTime CreatedAt { get; set; }
}
