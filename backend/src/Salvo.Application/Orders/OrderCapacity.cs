namespace Salvo.Application.Orders;

/// <summary>
/// How many orders this deployment is willing to hold.
/// </summary>
/// <param name="Maximum">
/// The ceiling, or <see langword="null"/> where the deployment does not set one.
/// </param>
/// <remarks>
/// <para>
/// <strong>Why a ceiling on the state and not only a limit on the rate.</strong> A rate limiter
/// bounds how often work is asked for. It does not bound how expensive that work is, and the
/// expensive one here — a scoring run — costs what it costs because of <em>how many orders there
/// are</em>, not how many times somebody asked. An import accepts up to ten thousand records and
/// five megabytes per file, as many times as one likes, so a public instance without this would let
/// a visitor make every later run slower for everybody, permanently, with a handful of requests
/// that no rate limit would find unusual.
/// </para>
/// <para>
/// Unlimited by default, so nothing outside the public instance changes. A local console, the
/// gate, the smoke and the screenshots all import freely.
/// </para>
/// </remarks>
public sealed record OrderCapacity(int? Maximum)
{
    /// <summary>No ceiling: what every deployment but the shared public one uses.</summary>
    public static OrderCapacity Unlimited { get; } = new((int?)null);
}
