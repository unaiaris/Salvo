namespace Salvo.Domain.External;

/// <summary>
/// What became of one callback message.
/// </summary>
/// <remarks>
/// There is deliberately no <c>DUPLICATE</c> member. A duplicate is the <em>absence</em> of a second
/// row: the deduplication key is unique within its provider, so a repeated message collides with the
/// receipt already stored instead of producing another one. A state that named the repetition would
/// contradict the very index that detects it.
/// </remarks>
public enum CallbackReceiptStatus
{
    /// <summary>The message moved the evaluation.</summary>
    Applied = 1,

    /// <summary>Correlated, and there was nothing to change.</summary>
    NoOp = 2,

    /// <summary>
    /// A non-terminal message about an evaluation that already has a verdict: it arrived out of
    /// order. The network, not the provider, is what went wrong.
    /// </summary>
    Superseded = 3,

    /// <summary>
    /// A <em>different</em> terminal message about an evaluation that already has a verdict: the
    /// provider is saying two things. Kept and shown rather than swallowed.
    /// </summary>
    Conflicting = 4,

    /// <summary>
    /// No evaluation could be correlated when the message arrived. It stays unprocessed until late
    /// linking finds the row it belongs to, which is what closes the window between the moment a
    /// provider answers and the moment that answer is written down.
    /// </summary>
    Unmatched = 5,
}
