namespace ApiGateway.Application.Gateway;

public sealed class GatePipeline(IEnumerable<IRequestGate> gates)
{
    private readonly IRequestGate[] _gates = gates.ToArray();

    public async Task<GateResult> RunAsync(GateContext context, CancellationToken cancellationToken)
    {
        foreach (var gate in _gates)
        {
            var result = await gate.EvaluateAsync(context, cancellationToken);
            if (!result.IsAllowed)
            {
                return result;
            }
        }

        return GateResult.Allow;
    }
}
