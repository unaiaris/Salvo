using System.Text.Json;
using System.Text.Json.Serialization;
using Salvo.Application.Orders.Seed;

namespace Salvo.Infrastructure.Seed;

public sealed class EmbeddedDemoOrderSource : IDemoOrderSource
{
    /// <summary>
    /// The file name carries the dataset version, so replacing the corpus is one edit in
    /// <see cref="DemoDatasetShape.Current"/> and the resource cannot drift from the expectation.
    /// </summary>
    private static readonly string ResourceName =
        $"Salvo.Infrastructure.Seed.Fixtures.demo-orders.v{DemoDatasetShape.Current.Version}.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    public async Task<DemoOrderDocument> LoadAsync(CancellationToken cancellationToken)
    {
        var assembly = typeof(EmbeddedDemoOrderSource).Assembly;
        await using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"The embedded demo fixture '{ResourceName}' is missing.");
        var document = await JsonSerializer.DeserializeAsync<DemoOrderDocument>(
            stream,
            SerializerOptions,
            cancellationToken);

        return document
            ?? throw new InvalidOperationException("The embedded demo fixture is empty.");
    }
}
