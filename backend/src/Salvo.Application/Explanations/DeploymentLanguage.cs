using Salvo.Domain.Explanations;

namespace Salvo.Application.Explanations;

/// <summary>
/// The one language this deployment writes explanations in and renders its console in.
/// </summary>
/// <param name="Value">The language, already validated when the process started.</param>
/// <remarks>
/// <para>
/// A single registered value rather than a setting each consumer reads for itself, and that is the
/// point of the type. The paragraph is written by the API and the surrounding screen is composed by
/// the console; if the two read the same variable separately, a deployment configured wrongly in
/// one of the two places shows a Portuguese console around a Spanish paragraph, and nothing fails.
/// The API declares this in <c>GET /api/system/capabilities</c> and the console takes it from
/// there, which makes that disagreement unrepresentable rather than unlikely.
/// </para>
/// <para>
/// It never comes from the request. <c>Accept-Language</c> would make the identity of a stored
/// explanation depend on who asked for it first, so one evaluation would accumulate a row per
/// reader.
/// </para>
/// </remarks>
public sealed record DeploymentLanguage(ExplanationLanguage Value)
{
    /// <summary>The default, and the language of the demonstration.</summary>
    public static readonly DeploymentLanguage Spanish = new(ExplanationLanguage.Spanish);

    /// <summary>The wire form, which is what the capabilities endpoint publishes.</summary>
    public string Wire => ExplanationWireNames.ToWire(Value);
}
