namespace Salvo.Application.Orders.Seed;

/// <summary>
/// The shape of the demo corpus this build ships, declared instead of hard-coded at the point of
/// use.
/// </summary>
/// <remarks>
/// <para>
/// Before stage 9 the seed asserted literal counts — version <c>"1"</c>, exactly 300 orders,
/// exactly 18 fraud labels — inside the handler, and it asserted them <strong>before</strong>
/// looking at the database. A corpus with a different number of frauds, which is the whole point of
/// enriching the fixture, failed with an <see cref="InvalidOperationException"/> that the endpoint
/// does not catch: a bare <c>500</c>, even against an empty database, where the honest answer is
/// either "loaded" or "this database has the previous corpus".
/// </para>
/// <para>
/// The version also names the embedded resource, so there is one place to change when the corpus is
/// replaced and no way for the file and the expectation to drift apart.
/// </para>
/// </remarks>
/// <param name="OrderCount">Orders the fixture must contain, exactly.</param>
/// <param name="FraudLabelCount">
/// Ground-truth frauds it must contain, exactly. Checked rather than counted so that a fixture
/// edited by hand cannot silently change what every quality metric is measured against.
/// </param>
public sealed record DemoDatasetShape(string Version, int OrderCount, int FraudLabelCount)
{
    /// <summary>
    /// The corpus of stage 9: 300 orders over the same three merchants and the same reference
    /// space, with 28 frauds of seven archetypes, deliberately including fraud the deterministic
    /// rules cannot see and legitimate orders they do flag.
    /// </summary>
    public static DemoDatasetShape Current { get; } = new("2", 300, 28);
}
