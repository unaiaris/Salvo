namespace Salvo.Domain.External;

/// <summary>
/// The external antifraud provider an evaluation was requested from.
/// </summary>
/// <remarks>
/// Deliberately separate from <c>RiskEvaluationSource</c>. The local evaluation is a pure function
/// of the corpus and this is a conversation with a remote system; sharing one enumeration would
/// reintroduce at the type level the mixture that separate tables exist to prevent.
/// </remarks>
public enum ExternalProvider
{
    ExternalMock = 1,
    KoinSandbox = 2,
}
