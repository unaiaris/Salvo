namespace Salvo.Domain.Explanations;

/// <summary>
/// The language an explanation is written in.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Part of the identity of the row</strong>, and that is the whole reason this is a domain
/// type rather than a formatting detail of the console. The paragraph is composed by the backend
/// and stored; a deployment that changes language has to be able to find that there is no text yet
/// in the language it now writes, and write one. Without the language in the identity it finds the
/// Spanish row, takes it for the answer, and never writes the Portuguese one — which is the defect
/// <c>E7D</c> was opened to fix, with <c>templateVersion</c> in the place this now occupies.
/// </para>
/// <para>
/// It is a property of the <em>deployment</em> and never of the request. Deriving it from
/// <c>Accept-Language</c> would make the identity of a stored row depend on who happened to ask
/// first, and two readers of one console would quietly produce two rows for one evaluation. Per
/// person is a post-MVP question that needs authentication to even be asked: without an identity
/// there is nobody to ask.
/// </para>
/// <para>
/// Two members, and both are load-bearing. A switch with one position is a switch nobody has
/// proved: neither «the language is of the deployment» nor «the language is in the identity» can
/// be demonstrated — or tested — while there is only one value it can take.
/// </para>
/// </remarks>
public enum ExplanationLanguage
{
    /// <summary>Castilian Spanish. The default, and the language of the demonstration.</summary>
    Spanish = 1,

    /// <summary>Brazilian Portuguese.</summary>
    Portuguese = 2,
}
