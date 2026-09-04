using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Salvo.Application.External;
using Salvo.Domain.External;

namespace Salvo.Infrastructure.External;

/// <summary>
/// The provider of the MVP: deterministic, in process, and without a network.
/// </summary>
/// <remarks>
/// <para>
/// The outcome is a documented function of the order reference — the trailing digits of
/// <c>merchantReferenceId</c>, modulo one hundred — with declared bands. A uniform hash over four
/// outcomes would leave roughly a quarter of the corpus pending, which is seventy-five manual
/// closures before a demo ends, and it would scatter the divergences at random. With bands, whoever
/// writes the fixture decides the distribution.
/// </para>
/// <para>
/// On <c>demo-orders.v1.json</c>, whose references run from <c>ORD_000001</c> to <c>ORD_000300</c>,
/// every remainder appears exactly three times, so the bands produce 225 approved, 45 denied, 21
/// pending and 9 in error. A test pins those four numbers.
/// </para>
/// </remarks>
public sealed class MockAntifraudProvider(
    MockAntifraudProviderOptions options,
    TimeProvider timeProvider) : IAntifraudProvider
{
    /// <summary>Approved for a remainder below this.</summary>
    public const int ApprovedBelow = 75;

    /// <summary>Denied for a remainder below this and not approved.</summary>
    public const int DeniedBelow = 90;

    /// <summary>Pending for a remainder below this and neither approved nor denied.</summary>
    public const int PendingBelow = 97;

    /// <summary>The one remainder that models a request that never left the process.</summary>
    public const int UnreachableRemainder = 97;

    private const int Modulus = 100;

    /// <summary>
    /// At most this many trailing digits are read, so a pathological reference cannot overflow the
    /// parse.
    /// </summary>
    private const int MaximumSuffixDigits = 9;

    public ExternalProvider Provider => ExternalProvider.ExternalMock;

    public async Task<ExternalEvaluationResult> EvaluateAsync(
        ExternalEvaluationInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        await DelayAsync(cancellationToken);

        return Compose(input.ReferenceId, resolvePending: false);
    }

    /// <summary>
    /// The same function, with the pending band resolved. Reconciliation exists to finish a pending
    /// evaluation, so a provider whose second answer were pending again would leave it stuck forever.
    /// </summary>
    public async Task<ExternalEvaluationResult> GetStatusAsync(
        ExternalEvaluationLookup lookup,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(lookup);
        await DelayAsync(cancellationToken);

        return Compose(lookup.ReferenceId, resolvePending: true);
    }

    /// <summary>
    /// The band of a reference: its trailing digits modulo one hundred.
    /// </summary>
    /// <remarks>
    /// A reference with no trailing digits — which the fixture never produces, but an import can —
    /// falls back to the first four bytes of its SHA-256. A managed string hash would not do: it is
    /// randomised per process, so the same order would land in different bands on every run.
    /// </remarks>
    public static int BandOf(string referenceId)
    {
        ArgumentNullException.ThrowIfNull(referenceId);

        var end = referenceId.Length;
        var start = end;
        while (start > 0 && char.IsAsciiDigit(referenceId[start - 1]) && end - start < MaximumSuffixDigits)
        {
            start--;
        }

        if (start < end)
        {
            return (int)(long.Parse(referenceId[start..end], CultureInfo.InvariantCulture) % Modulus);
        }

        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(referenceId));

        return (int)(BitConverter.ToUInt32(digest, 0) % Modulus);
    }

    private static ExternalEvaluationResult Compose(string referenceId, bool resolvePending)
    {
        var band = BandOf(referenceId);
        var identifier = Identifier(referenceId);

        if (band < ApprovedBelow)
        {
            return new(ExternalProviderOutcome.Approved, identifier, Score: band);
        }

        if (band < DeniedBelow)
        {
            return new(ExternalProviderOutcome.Denied, identifier, Score: band);
        }

        if (band < PendingBelow)
        {
            if (!resolvePending)
            {
                return new(ExternalProviderOutcome.Pending, identifier);
            }

            // A pending evaluation settles into one of the two verdicts by the parity of its own
            // remainder, so the fixture still controls which orders end up diverging from the local
            // criterion.
            return band % 2 == 0
                ? new(ExternalProviderOutcome.Approved, identifier, Score: band)
                : new(ExternalProviderOutcome.Denied, identifier, Score: band);
        }

        // The two settling failures, one of each kind: a request that never left, and a request the
        // provider refused outright. Both end in ERROR, and only these two do.
        return band == UnreachableRemainder
            ? new(ExternalProviderOutcome.Unreachable)
            : new(ExternalProviderOutcome.Rejected, identifier);
    }

    /// <summary>
    /// The identifier the mock assigns, derived from the reference so that a second answer about
    /// the same order carries the same one.
    /// </summary>
    private static string Identifier(string referenceId)
    {
        return $"MOCK-{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(referenceId)))[..16]}";
    }

    private async Task DelayAsync(CancellationToken cancellationToken)
    {
        if (options.SimulatedLatency > TimeSpan.Zero)
        {
            await Task.Delay(options.SimulatedLatency, timeProvider, cancellationToken);
        }
    }
}
