using System.Text.RegularExpressions;

namespace ApiGateway.Application.Analytics;

public static partial class EndpointPattern
{
    public static string Of(string path) => IdSegment().Replace(path, "/{id}");

    // numbers, GUIDs and long hex strings
    [GeneratedRegex(@"/(\d+|[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}|[0-9a-fA-F]{16,})(?=/|$)")]
    private static partial Regex IdSegment();
}
