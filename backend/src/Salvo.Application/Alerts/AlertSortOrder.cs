namespace Salvo.Application.Alerts;

/// <summary>
/// How the alert feed is ordered.
/// </summary>
public enum AlertSortOrder
{
    /// <summary>
    /// Newest alert first. The default, and the only order that does not depend on the current
    /// scoring run.
    /// </summary>
    CreatedDesc = 1,

    /// <summary>
    /// Highest local score of the current evaluation first.
    /// </summary>
    LocalScoreDesc = 2,
}

/// <summary>
/// Wire names of <see cref="AlertSortOrder"/>.
/// </summary>
/// <remarks>
/// <see cref="ScoreDesc"/> means the <em>local</em> score of the evaluation the current run made
/// current, and nothing else. From stage 6 on an order will also have external evaluations with
/// scores of their own, and a value called "current score" would stop having one meaning.
/// </remarks>
public static class AlertSortWireNames
{
    public const string CreatedDesc = "CREATED_DESC";
    public const string ScoreDesc = "SCORE_DESC";

    public static bool TryParse(string? value, out AlertSortOrder sort)
    {
        switch (value)
        {
            case CreatedDesc:
                sort = AlertSortOrder.CreatedDesc;
                return true;
            case ScoreDesc:
                sort = AlertSortOrder.LocalScoreDesc;
                return true;
            default:
                sort = default;
                return false;
        }
    }
}
