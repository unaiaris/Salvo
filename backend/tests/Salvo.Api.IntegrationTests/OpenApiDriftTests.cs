using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Salvo.Api.IntegrationTests;

/// <summary>
/// The captured OpenAPI document has to be what this API actually serves.
/// </summary>
/// <remarks>
/// <para>
/// The gate already has one half of this: <c>npm run api:types:check</c> regenerates
/// <c>src/lib/api/schema.d.ts</c> from <c>frontend/openapi/salvo-openapi.json</c> and fails on a
/// mismatch. But that only proves the committed types agree with the committed document. Nothing
/// checked the document against the API, and the failure that gap allows is the quiet one: a field
/// renamed in a view, the document not recaptured, and a console whose guards project a key the API
/// no longer sends — which surfaces as a `malformed` failure on screen rather than as a red gate.
/// </para>
/// <para>
/// Capture is a manual step for a reason: it needs a listening API and the gate has none. This test
/// is the other side of that bargain — the capture stays manual, and forgetting it stops being
/// silent. When it fails, the fix is to run the API with <c>DemoData__Enabled=true</c> and
/// <c>npm run api:capture</c>, then <c>npm run api:types</c>, and commit both files.
/// </para>
/// </remarks>
public sealed class OpenApiDriftTests
{
    /// <summary>
    /// Both documents are compared with the <c>servers</c> entry replaced by the same placeholder
    /// the capture script writes. It echoes whatever host and port the process happened to be
    /// listening on — <c>http://localhost</c> under <see cref="SalvoApiFactory"/> — and that is not
    /// part of the contract.
    /// </summary>
    private const string ServersPlaceholder = """[{"url":"/"}]""";

    [Fact]
    public async Task TheCapturedDocumentMatchesTheOneTheApiServes()
    {
        // Demo data on: the seed and the metrics routes only exist under that flag, and the captured
        // document is the superset. Comparing against an API without it would report every demo
        // route as drift.
        await using var factory = new SalvoApiFactory { DemoDataEnabled = true };
        using var client = await factory.CreateMigratedClientAsync();

        var response = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var served = Normalize(JsonNode.Parse(await response.Content.ReadAsStringAsync()));
        var capturedPath = CapturedDocumentPath();
        var captured = Normalize(JsonNode.Parse(await File.ReadAllTextAsync(capturedPath)));

        // Paths and schemas first: a name-level difference is the readable half of the failure, and
        // a deep comparison alone would only say "they differ".
        AssertSameKeys("paths", served, captured);
        AssertSameSchemas(served, captured);

        Assert.True(
            JsonNode.DeepEquals(served, captured),
            $"""
             The OpenAPI document this API serves is not the one committed at {capturedPath}.

             Re-capture it:
               DemoData__Enabled=true dotnet run --project backend/src/Salvo.Api
               npm run api:capture --prefix frontend
               npm run api:types --prefix frontend

             and commit both the document and the regenerated types.
             """);
    }

    /// <summary>
    /// The document is the superset, so it must carry the routes that only exist under the demo
    /// flag. A capture taken against an API without them would silently shrink the contract the
    /// console is generated from.
    /// </summary>
    [Fact]
    public async Task TheCapturedDocumentIsTheSupersetThatIncludesTheDemoRoutes()
    {
        var captured = Normalize(JsonNode.Parse(await File.ReadAllTextAsync(CapturedDocumentPath())));
        var paths = captured?["paths"]?.AsObject().Select(entry => entry.Key).ToHashSet(StringComparer.Ordinal)
            ?? [];

        Assert.Contains("/api/demo-data/seed", paths);
        Assert.Contains("/api/evaluation-metrics", paths);
    }

    private static void AssertSameKeys(string section, JsonNode? served, JsonNode? captured)
    {
        var servedKeys = KeysOf(served?[section]);
        var capturedKeys = KeysOf(captured?[section]);

        Assert.Equal(capturedKeys, servedKeys);
    }

    private static void AssertSameSchemas(JsonNode? served, JsonNode? captured)
    {
        var servedKeys = KeysOf(served?["components"]?["schemas"]);
        var capturedKeys = KeysOf(captured?["components"]?["schemas"]);

        Assert.Equal(capturedKeys, servedKeys);
    }

    private static IReadOnlyList<string> KeysOf(JsonNode? node)
    {
        return node is JsonObject candidate
            ? [.. candidate.Select(entry => entry.Key).Order(StringComparer.Ordinal)]
            : [];
    }

    /// <summary>
    /// Reparses through a canonical serialization so that key order and number formatting cannot
    /// make two equivalent documents compare as different, and replaces the environment-dependent
    /// <c>servers</c> entry.
    /// </summary>
    private static JsonNode? Normalize(JsonNode? document)
    {
        if (document is null)
        {
            return null;
        }

        document["servers"] = JsonNode.Parse(ServersPlaceholder);

        return JsonNode.Parse(document.ToJsonString(new JsonSerializerOptions { WriteIndented = false }));
    }

    /// <summary>
    /// Walks up from the test binaries to the repository root, identified by the solution file. The
    /// test has no notion of a working directory it can rely on, and hard-coding a relative depth
    /// would break the first time the output path changes.
    /// </summary>
    private static string CapturedDocumentPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Salvo.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);

        var path = Path.Combine(directory.FullName, "frontend", "openapi", "salvo-openapi.json");
        Assert.True(File.Exists(path), $"The captured OpenAPI document is missing at {path}.");

        return path;
    }
}
