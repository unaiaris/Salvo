namespace Salvo.Api;

/// <summary>
/// Which parts of the demonstration this deployment registers.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Why there are two switches and not one.</strong> <c>DemoData:Enabled</c> used to govern
/// four things at once: the seed route, the quality metrics, the external provider triggers, and
/// what the console is told it can offer. Turning it off to protect the public instance would have
/// taken the quality surface with it — F1, the confusion matrix, the threshold sweep — and that
/// surface is half of what the project argues.
/// </para>
/// <para>
/// The public instance needs exactly one of those four gone. Its database arrives baked, so nobody
/// there needs to seed; and the seed route was the only one with which a visitor could leave the
/// console unusable for the next one, because loading a corpus over a database that already holds
/// orders with those references is refused and a corpus loaded twice the size is not. The restart
/// bounds that damage in time; removing the route means it does not happen.
/// </para>
/// <para>
/// <strong>The seed switch narrows, never widens.</strong> It is only ever consulted when
/// <c>DemoData:Enabled</c> is already on, and it defaults to on, so every existing way of running
/// this API behaves exactly as before — the gate, the smoke, the screenshots and
/// <c>scripts/demo.sh</c> declare one variable and get the whole demonstration. The container
/// declares the second one.
/// </para>
/// </remarks>
internal static class DemoDataSwitches
{
    /// <summary>
    /// Whether this deployment declares itself a demonstration at all: the quality metrics, the
    /// provider triggers, and the capabilities the console reads.
    /// </summary>
    public static bool Enabled(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return configuration.GetValue<bool>("DemoData:Enabled");
    }

    /// <summary>
    /// Whether <c>POST /api/demo-data/seed</c> and its preview are registered.
    /// </summary>
    public static bool SeedEnabled(IConfiguration configuration)
    {
        return Enabled(configuration) && configuration.GetValue("DemoData:SeedEnabled", true);
    }
}
