using Salvo.Domain.Alerts;

namespace Salvo.Domain.Tests;

public sealed class AlertTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 9, 2, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AnOpenedAlertDerivesItsSeverityAndNormalizesItsTimestamp()
    {
        var alert = CreateAlert(90, new DateTimeOffset(2026, 9, 2, 9, 0, 0, TimeSpan.FromHours(-3)));

        Assert.Equal(AlertStatus.Open, alert.Status);
        Assert.Equal(AlertSeverity.Critical, alert.Severity);
        Assert.Equal("e4-v1", alert.AlertPolicyVersion);
        Assert.Equal(CreatedAt, alert.CreatedAt);
        Assert.Equal(TimeSpan.Zero, alert.CreatedAt.Offset);
        Assert.Null(alert.ReviewedAt);
        Assert.Null(alert.SupersedesAlertId);
    }

    [Fact]
    public void AlertRejectsScoresOutsideEveryBandAndSelfSupersession()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateAlert(59, CreatedAt));

        var id = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => Alert.Open(
            id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            60,
            "[]",
            "e4-v1",
            id,
            CreatedAt));
    }

    [Fact]
    public void ReviewRecordsTheVerdictOnceAndRefusesEverySecondOne()
    {
        var alert = CreateAlert(70, CreatedAt);
        var reviewId = Guid.NewGuid();

        var review = alert.Review(reviewId, AlertStatus.ReportedFraud, "  chargeback confirmed  ", null, CreatedAt.AddHours(2));

        Assert.Equal(AlertStatus.ReportedFraud, alert.Status);
        Assert.Equal(CreatedAt.AddHours(2), alert.ReviewedAt);
        Assert.Equal(reviewId, review.Id);
        Assert.Equal(alert.Id, review.AlertId);
        Assert.Equal(AlertStatus.Open, review.PreviousStatus);
        Assert.Equal(AlertStatus.ReportedFraud, review.NewStatus);
        Assert.Equal("  chargeback confirmed  ", review.Note);

        // A verdict is terminal by design: nothing reopens it, not even the same verdict again.
        Assert.Throws<AlertTransitionException>(() =>
            alert.Review(Guid.NewGuid(), AlertStatus.ConfirmedSafe, null, null, CreatedAt.AddHours(3)));
        Assert.Throws<AlertTransitionException>(() =>
            alert.Review(Guid.NewGuid(), AlertStatus.ReportedFraud, null, null, CreatedAt.AddHours(3)));
        Assert.Equal(AlertStatus.ReportedFraud, alert.Status);
    }

    [Fact]
    public void ReviewRefusesAStatusThatIsNotAVerdict()
    {
        var alert = CreateAlert(60, CreatedAt);

        Assert.Throws<AlertTransitionException>(() =>
            alert.Review(Guid.NewGuid(), AlertStatus.Open, null, null, CreatedAt));
        Assert.Equal(AlertStatus.Open, alert.Status);
    }

    private static Alert CreateAlert(int score, DateTimeOffset createdAt)
    {
        return Alert.Open(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            score,
            "[]",
            "e4-v1",
            supersedesAlertId: null,
            createdAt);
    }
}
