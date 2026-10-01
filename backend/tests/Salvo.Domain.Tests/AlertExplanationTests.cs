using Salvo.Domain.Explanations;

namespace Salvo.Domain.Tests;

/// <summary>
/// What the row of an explanation keeps about what it cost.
/// </summary>
/// <remarks>
/// Decision 76: the tokens are kept in every outcome and added up across attempts. Before it, only an
/// accepted text recorded its tokens and a retry erased the previous ones, so what the row measured
/// was the cost of the cheap subset — the refusals, the texts cut at the budget and the figures the
/// verifier rejected were all paid for and none of them left a trace.
/// </remarks>
public sealed class AlertExplanationTests
{
    private static readonly DateTimeOffset RequestedAt = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AFailedAttemptKeepsWhatItWasChargedFor()
    {
        var explanation = Reserve();

        explanation.Fail(ExplanationFailureCode.ProviderRefused, null, RequestedAt.AddSeconds(2), 412, 0);

        Assert.Equal(ExplanationStatus.Failed, explanation.Status);
        Assert.Equal(412, explanation.InputTokens);
        Assert.Equal(0, explanation.OutputTokens);
    }

    /// <summary>
    /// A paragraph accepted on the third attempt cost three calls, and the row says so.
    /// </summary>
    [Fact]
    public void TheTokensOfEveryAttemptAddUpOnTheSameRow()
    {
        var explanation = Reserve();

        explanation.Fail(ExplanationFailureCode.NotGroundedNumber, "7", RequestedAt.AddSeconds(2), 1500, 300);
        explanation.Retake(Guid.NewGuid(), RequestedAt.AddMinutes(1));

        // A new attempt starts from what the earlier ones cost, not from nothing.
        Assert.Equal(ExplanationStatus.Pending, explanation.Status);
        Assert.Equal(1500, explanation.InputTokens);
        Assert.Equal(300, explanation.OutputTokens);

        explanation.Fail(ExplanationFailureCode.MalformedOutput, "max_tokens", RequestedAt.AddMinutes(2), 1500, 1024);
        explanation.Retake(Guid.NewGuid(), RequestedAt.AddMinutes(3));
        explanation.Complete("El pedido superó el umbral.", ["velocity"], "claude-sonnet-5-5", 1510, 280, RequestedAt.AddMinutes(4));

        Assert.Equal(3, explanation.AttemptCount);
        Assert.Equal(4510, explanation.InputTokens);
        Assert.Equal(1604, explanation.OutputTokens);
    }

    /// <summary>
    /// A writer that reports no counts — the template — leaves the columns empty rather than zero:
    /// zero would claim a measurement nobody made.
    /// </summary>
    [Fact]
    public void AWriterThatReportsNothingLeavesTheCountUnknown()
    {
        var written = Reserve();
        written.Complete("El pedido superó el umbral.", ["velocity"], null, null, null, RequestedAt.AddSeconds(1));

        var failed = Reserve();
        failed.Fail(ExplanationFailureCode.ProviderUnavailable, null, RequestedAt.AddSeconds(1));

        Assert.Null(written.InputTokens);
        Assert.Null(written.OutputTokens);
        Assert.Null(failed.InputTokens);
        Assert.Null(failed.OutputTokens);
    }

    [Fact]
    public void ANegativeCountIsRefused()
    {
        var explanation = Reserve();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            explanation.Fail(ExplanationFailureCode.ProviderRefused, null, RequestedAt, -1, 0));
    }

    private static AlertExplanation Reserve()
    {
        return AlertExplanation.Reserve(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExplanationProvider.Anthropic,
            "anthropic-p1",
            "e4-v1",
            ExplanationLanguage.Spanish,
            Guid.NewGuid(),
            RequestedAt);
    }
}
