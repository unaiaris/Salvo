namespace Salvo.Domain.External;

/// <summary>
/// Stable textual names for <see cref="CallbackReceiptStatus"/>, so the database and the HTTP
/// contract cannot drift apart from each other or from the enumeration.
/// </summary>
public static class CallbackReceiptWireNames
{
    public const string Applied = "APPLIED";
    public const string NoOp = "NO_OP";
    public const string Superseded = "SUPERSEDED";
    public const string Conflicting = "CONFLICTING";
    public const string Unmatched = "UNMATCHED";

    public static string ToWire(CallbackReceiptStatus status)
    {
        return status switch
        {
            CallbackReceiptStatus.Applied => Applied,
            CallbackReceiptStatus.NoOp => NoOp,
            CallbackReceiptStatus.Superseded => Superseded,
            CallbackReceiptStatus.Conflicting => Conflicting,
            CallbackReceiptStatus.Unmatched => Unmatched,
            _ => throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Unsupported callback receipt status."),
        };
    }

    public static CallbackReceiptStatus Parse(string value)
    {
        return value switch
        {
            Applied => CallbackReceiptStatus.Applied,
            NoOp => CallbackReceiptStatus.NoOp,
            Superseded => CallbackReceiptStatus.Superseded,
            Conflicting => CallbackReceiptStatus.Conflicting,
            Unmatched => CallbackReceiptStatus.Unmatched,
            _ => throw new ArgumentException(
                $"Unsupported callback receipt status '{value}'.",
                nameof(value)),
        };
    }
}
