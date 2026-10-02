namespace ApiGateway.Application.Gateway;

public interface IRequestGate
{
    Task<GateResult> EvaluateAsync(GateContext context, CancellationToken cancellationToken);
}
