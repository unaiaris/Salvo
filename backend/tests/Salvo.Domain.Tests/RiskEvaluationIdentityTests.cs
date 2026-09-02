using System.Reflection;
using Salvo.Domain.Risk;

namespace Salvo.Domain.Tests;

public sealed class RiskEvaluationIdentityTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 9, 2, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void LocalEvaluationHashesExactlyTheStringItPersists()
    {
        var assessment = RiskSignalSerializerTests.ScoreOrderTriggeringSeveralRules();

        var evaluation = RiskEvaluation.ForLocal(Guid.NewGuid(), "e3-v1", assessment, CreatedAt);

        Assert.Equal(RiskEvaluationSource.Local, evaluation.Source);
        Assert.Equal(assessment.OrderId, evaluation.OrderId);
        Assert.Equal(assessment.Score, evaluation.Score);
        Assert.Equal("e3-v1", evaluation.RuleConfigVersion);
        Assert.Equal(RiskSignalSerializer.Serialize(assessment.Signals), evaluation.SignalsJson);
        Assert.Equal(
            RiskEvaluationFingerprint.Compute(
                assessment.OrderId,
                RiskEvaluationSource.Local,
                "e3-v1",
                assessment.Score,
                evaluation.SignalsJson!),
            evaluation.EvaluationFingerprint);
        Assert.Equal(RiskEvaluationFingerprint.Length, evaluation.EvaluationFingerprint!.Length);
        Assert.All(evaluation.EvaluationFingerprint, character =>
            Assert.True(
                char.IsAsciiDigit(character) || character is >= 'a' and <= 'f',
                $"'{character}' is not a lowercase hexadecimal digit."));
        Assert.Null(evaluation.ExternalEvaluationId);
        Assert.Null(evaluation.ErrorCode);
    }

    [Fact]
    public void FingerprintIsStableAcrossCultures()
    {
        var assessment = RiskSignalSerializerTests.ScoreOrderTriggeringSeveralRules();
        var expected = RiskEvaluation
            .ForLocal(Guid.NewGuid(), "e3-v1", assessment, CreatedAt)
            .EvaluationFingerprint;

        var fingerprints = CultureProbe.Run(
            ["en-US", "es-UY", "de-DE"],
            () => RiskEvaluation
                .ForLocal(Guid.NewGuid(), "e3-v1", assessment, CreatedAt)
                .EvaluationFingerprint);

        Assert.All(fingerprints, actual => Assert.Equal(expected, actual));
    }

    [Fact]
    public void FingerprintIdentifiesContentAndNotTheRow()
    {
        var assessment = RiskSignalSerializerTests.ScoreOrderTriggeringSeveralRules();
        var baseline = RiskEvaluation.ForLocal(Guid.NewGuid(), "e3-v1", assessment, CreatedAt);
        var sameContent = RiskEvaluation.ForLocal(
            Guid.NewGuid(),
            "e3-v1",
            assessment,
            CreatedAt.AddDays(3));

        Assert.NotEqual(baseline.Id, sameContent.Id);
        Assert.Equal(baseline.EvaluationFingerprint, sameContent.EvaluationFingerprint);

        var otherOrder = new LocalRiskAssessment(
            Guid.NewGuid(),
            assessment.OccurredAt,
            assessment.Score,
            assessment.IsFlagged,
            assessment.Signals);
        var otherScore = new LocalRiskAssessment(
            assessment.OrderId,
            assessment.OccurredAt,
            assessment.Score - 10,
            assessment.IsFlagged,
            assessment.Signals);
        var otherSignals = new LocalRiskAssessment(
            assessment.OrderId,
            assessment.OccurredAt,
            assessment.Score,
            assessment.IsFlagged,
            [.. assessment.Signals.Take(1)]);

        Assert.NotEqual(
            baseline.EvaluationFingerprint,
            RiskEvaluation.ForLocal(Guid.NewGuid(), "e3-v1", otherOrder, CreatedAt).EvaluationFingerprint);
        Assert.NotEqual(
            baseline.EvaluationFingerprint,
            RiskEvaluation.ForLocal(Guid.NewGuid(), "e3-v1", otherScore, CreatedAt).EvaluationFingerprint);
        Assert.NotEqual(
            baseline.EvaluationFingerprint,
            RiskEvaluation.ForLocal(Guid.NewGuid(), "e3-v1", otherSignals, CreatedAt).EvaluationFingerprint);
        Assert.NotEqual(
            baseline.EvaluationFingerprint,
            RiskEvaluation.ForLocal(Guid.NewGuid(), "e3-v2", assessment, CreatedAt).EvaluationFingerprint);
    }

    [Fact]
    public void LocalStatusMirrorsTheDeterministicFlag()
    {
        var orderId = Guid.NewGuid();
        var flagged = RiskEvaluation.ForLocal(
            Guid.NewGuid(),
            "e3-v1",
            new(orderId, CreatedAt, 60, true, [new(RiskRuleNames.AmountAnomaly, 40, "detail")]),
            CreatedAt);
        var clean = RiskEvaluation.ForLocal(
            Guid.NewGuid(),
            "e3-v1",
            new(orderId, CreatedAt, 0, false, []),
            CreatedAt);

        Assert.Equal(RiskEvaluationStatus.Denied, flagged.Status);
        Assert.True(flagged.IsFlagged);
        Assert.Equal(RiskEvaluationStatus.Approved, clean.Status);
        Assert.False(clean.IsFlagged);
        Assert.Equal("[]", clean.SignalsJson);
    }

    [Fact]
    public void AppendOnlyEntitiesExposeNoPublicMutator()
    {
        foreach (var type in new[] { typeof(RiskEvaluation), typeof(ScoringRun), typeof(RunEvaluation) })
        {
            Assert.All(
                type.GetProperties(BindingFlags.Public | BindingFlags.Instance),
                property => Assert.False(
                    property.SetMethod?.IsPublic ?? false,
                    $"{type.Name}.{property.Name} exposes a public setter."));
            Assert.Empty(type.GetFields(BindingFlags.Public | BindingFlags.Instance));
        }
    }

    [Fact]
    public void EvaluationRejectsInputThatCannotIdentifyItself()
    {
        var assessment = RiskSignalSerializerTests.ScoreOrderTriggeringSeveralRules();

        Assert.Throws<ArgumentException>(() =>
            RiskEvaluation.ForLocal(Guid.Empty, "e3-v1", assessment, CreatedAt));
        Assert.Throws<ArgumentException>(() =>
            RiskEvaluation.ForLocal(Guid.NewGuid(), " ", assessment, CreatedAt));
        Assert.Throws<ArgumentException>(() =>
            RiskEvaluation.ForLocal(
                Guid.NewGuid(),
                "e3-v1",
                new(Guid.Empty, CreatedAt, 0, false, []),
                CreatedAt));
    }
}
