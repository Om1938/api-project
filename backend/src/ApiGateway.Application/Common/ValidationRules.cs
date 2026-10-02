using FluentValidation;

namespace ApiGateway.Application.Common;

public static class ValidationRules
{
    public static IRuleBuilderOptions<T, string> HttpUrl<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .MaximumLength(2048)
            .Must(value => Uri.TryCreate(value, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("'{PropertyName}' must be an absolute http or https URL.");

    public static IRuleBuilderOptions<T, string> DisplayName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(100);
}
