namespace Salvo.Infrastructure.External;

/// <param name="SimulatedLatency">
/// How long the mock pretends to take. Zero by default: a demo that waits is a demo nobody runs.
/// </param>
public sealed record MockAntifraudProviderOptions(TimeSpan SimulatedLatency)
{
    public static readonly MockAntifraudProviderOptions Default = new(TimeSpan.Zero);
}
