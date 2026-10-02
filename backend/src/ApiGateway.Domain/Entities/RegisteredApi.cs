namespace ApiGateway.Domain.Entities;

public sealed class RegisteredApi : Entity
{
    private RegisteredApi() { }

    public RegisteredApi(Guid ownerId, string name, string slug, string? description, string targetBaseUrl)
    {
        OwnerId = ownerId;
        Slug = slug;
        Update(name, description, targetBaseUrl, isActive: true);
    }

    public Guid OwnerId { get; private set; }
    public string Name { get; private set; } = null!;

    public string Slug { get; private set; } = null!;

    public string? Description { get; private set; }
    public string TargetBaseUrl { get; private set; } = null!;
    public bool IsActive { get; private set; }

    public void Update(string name, string? description, string targetBaseUrl, bool isActive)
    {
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        TargetBaseUrl = targetBaseUrl.Trim().TrimEnd('/');
        IsActive = isActive;
    }
}
