using Salvo.Domain.Risk;

namespace Salvo.Domain.Alerts;

/// <summary>
/// The immutable function that maps a risk score to an alert severity, versioned exactly like
/// <see cref="RuleConfig"/>.
/// </summary>
/// <remarks>
/// The derived severity is never persisted; <see cref="Alert.AlertPolicyVersion"/> is. Storing the
/// version of the function instead of its result keeps a future policy from silently reclassifying
/// historical alerts, including the ones an analyst already reviewed.
/// </remarks>
public sealed class AlertPolicy
{
    public const string E4V1Version = "e4-v1";

    private readonly AlertSeverityBand[] bands;

    public AlertPolicy(string version, IReadOnlyList<AlertSeverityBand> bands)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentNullException.ThrowIfNull(bands);

        Version = version;
        this.bands = [.. bands];
    }

    /// <summary>
    /// The approved policy of stage 4. Its floor is tied to <see cref="RuleConfig.FlagThreshold"/>
    /// by <see cref="Validate"/>, which the application invokes at startup.
    /// </summary>
    public static AlertPolicy E4V1 { get; } = new(
        E4V1Version,
        [
            new(60, 69, AlertSeverity.Medium),
            new(70, 89, AlertSeverity.High),
            new(90, 100, AlertSeverity.Critical),
        ]);

    /// <summary>
    /// Every policy an alert may reference. An alert resolves its severity through the version it
    /// was created with, so retiring a version is a data migration, not a code edit.
    /// </summary>
    public static IReadOnlyList<AlertPolicy> All { get; } = [E4V1];

    public string Version { get; }

    public IReadOnlyList<AlertSeverityBand> Bands => bands;

    public static AlertPolicy ForVersion(string version)
    {
        foreach (var policy in All)
        {
            if (string.Equals(policy.Version, version, StringComparison.Ordinal))
            {
                return policy;
            }
        }

        throw new ArgumentException($"Unknown alert policy version '{version}'.", nameof(version));
    }

    /// <summary>
    /// The severity <paramref name="score"/> falls into.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">No band covers the score.</exception>
    public AlertSeverity SeverityFor(int score)
    {
        foreach (var band in bands)
        {
            if (band.Contains(score))
            {
                return band.Severity;
            }
        }

        throw new ArgumentOutOfRangeException(
            nameof(score),
            score,
            $"Alert policy '{Version}' defines no severity band for this score.");
    }

    /// <summary>
    /// The severity of <paramref name="score"/>, or <see langword="null"/> when the score is below
    /// the alerting floor. Used to describe an evaluation that would no longer raise an alert.
    /// </summary>
    public AlertSeverity? SeverityForOrNull(int score)
    {
        foreach (var band in bands)
        {
            if (band.Contains(score))
            {
                return band.Severity;
            }
        }

        return null;
    }

    public AlertSeverityBand? BandFor(AlertSeverity severity)
    {
        foreach (var band in bands)
        {
            if (band.Severity == severity)
            {
                return band;
            }
        }

        return null;
    }

    /// <summary>
    /// Checks that the policy is a total function over the alertable score range of
    /// <paramref name="config"/>: it starts exactly at the flag threshold, has no gaps or overlaps,
    /// and reaches the score cap.
    /// </summary>
    /// <remarks>
    /// The floor is deliberately tied to <see cref="RuleConfig.FlagThreshold"/>. If a later rule
    /// configuration lowers the threshold to 50, this fails at startup instead of leaving a flagged
    /// score of 55 without a band.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The policy is not a valid banding.</exception>
    public void Validate(RuleConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (bands.Length == 0)
        {
            throw new InvalidOperationException($"Alert policy '{Version}' defines no severity band.");
        }

        if (bands[0].MinimumScore != config.FlagThreshold)
        {
            throw new InvalidOperationException(
                $"Alert policy '{Version}' starts at {bands[0].MinimumScore} but rule configuration "
                + $"'{config.Version}' flags from {config.FlagThreshold}.");
        }

        if (bands[^1].MaximumScore != config.ScoreCap)
        {
            throw new InvalidOperationException(
                $"Alert policy '{Version}' ends at {bands[^1].MaximumScore} but rule configuration "
                + $"'{config.Version}' caps the score at {config.ScoreCap}.");
        }

        for (var index = 0; index < bands.Length; index++)
        {
            var band = bands[index];
            if (band.MinimumScore > band.MaximumScore)
            {
                throw new InvalidOperationException(
                    $"Alert policy '{Version}' defines an empty band for {band.Severity}.");
            }

            if (index > 0 && band.MinimumScore != bands[index - 1].MaximumScore + 1)
            {
                throw new InvalidOperationException(
                    $"Alert policy '{Version}' leaves a gap or an overlap before {band.Severity}.");
            }

            if (index > 0 && band.Severity <= bands[index - 1].Severity)
            {
                throw new InvalidOperationException(
                    $"Alert policy '{Version}' must order its bands by increasing severity.");
            }
        }
    }
}
