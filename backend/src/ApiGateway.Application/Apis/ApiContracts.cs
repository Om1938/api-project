using ApiGateway.Application.Common;
using ApiGateway.Domain.Entities;
using FluentValidation;

namespace ApiGateway.Application.Apis;

public sealed record CreateApiRequest(string Name, string? Slug, string? Description, string TargetBaseUrl);

public sealed record UpdateApiRequest(string Name, string? Description, string TargetBaseUrl, bool IsActive);

public sealed record ApiDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string TargetBaseUrl,
    bool IsActive,
    DateTime CreatedAt)
{
    public static ApiDto From(RegisteredApi api) =>
        new(api.Id, api.Name, api.Slug, api.Description, api.TargetBaseUrl, api.IsActive, api.CreatedAt);
}

public sealed class CreateApiRequestValidator : AbstractValidator<CreateApiRequest>
{
    public CreateApiRequestValidator()
    {
        RuleFor(x => x.Name).DisplayName();
        RuleFor(x => x.Slug!)
            .MaximumLength(Slugs.MaxLength)
            .Must(Slugs.IsValid)
            .WithMessage("'Slug' may contain only lowercase letters, digits and single hyphens.")
            .When(x => !string.IsNullOrEmpty(x.Slug));
        RuleFor(x => x.Name)
            .Must(name => Slugs.From(name).Length > 0)
            .WithMessage("'Name' must contain at least one letter or digit.")
            .When(x => string.IsNullOrEmpty(x.Slug) && !string.IsNullOrWhiteSpace(x.Name));
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.TargetBaseUrl).HttpUrl();
    }
}

public sealed class UpdateApiRequestValidator : AbstractValidator<UpdateApiRequest>
{
    public UpdateApiRequestValidator()
    {
        RuleFor(x => x.Name).DisplayName();
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.TargetBaseUrl).HttpUrl();
    }
}
