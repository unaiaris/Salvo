using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;
using Salvo.Application.External;
using Salvo.Domain.External;

namespace Salvo.Api;

public static class ExternalCallbackEndpoints
{
    /// <summary>The header a caller presents the shared secret in.</summary>
    public const string SecretHeader = "X-Salvo-Callback-Secret";

    /// <summary>
    /// Where the shared secret is read from. Backend configuration only: it never reaches the Next
    /// process and never carries a <c>NEXT_PUBLIC_</c> prefix.
    /// </summary>
    public const string SecretConfigurationKey = "SALVO_CALLBACK_SHARED_SECRET";

    /// <summary>
    /// A callback is a handful of fields. The limit is explicit and small, so an oversized body is
    /// refused before it is read rather than after.
    /// </summary>
    public const int MaximumBodyBytes = 8 * 1024;

    private static readonly JsonSerializerOptions PayloadOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// The door an external provider knocks on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The route is always mapped, and it answers <c>401</c> to everything when no secret is
    /// configured. Mapping it conditionally would make the published OpenAPI document depend on the
    /// environment; answering "no secret, come in" would be the failure mode this endpoint exists to
    /// avoid. Failing closed is the only reading of an absent secret that is ever safe.
    /// </para>
    /// <para>
    /// The shared secret is <em>not</em> the real mechanism. Section 5.3 of the Blueprint requires
    /// verifying how Koin actually authenticates its callbacks, which is typically a signature over
    /// the body. What is modelled here is that the problem exists and where it is solved, not a
    /// solution fit for production.
    /// </para>
    /// </remarks>
    public static IEndpointRouteBuilder MapExternalCallbackEndpoints(
        this IEndpointRouteBuilder endpoints,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var secret = configuration[SecretConfigurationKey];

        endpoints.MapPost("/api/external-callbacks/{provider}", (
                string provider,
                HttpContext context,
                ApplyExternalCallbackHandler handler,
                CancellationToken cancellationToken) =>
                ReceiveAsync(provider, secret, context, handler, cancellationToken))
            .WithName("ReceiveExternalCallback")
            .WithTags("External evaluations")
            .WithDescription(
                "Receives a callback from an external antifraud provider. Authenticated with a "
                + "shared secret in the "
                + SecretHeader
                + " header, compared in constant time. With no secret configured every request is "
                + "refused: an unauthenticated callback endpoint would let anyone settle any "
                + "evaluation. Unknown fields in the payload are ignored, because a provider adds "
                + "fields without warning and that must not break reception. A duplicate is "
                + "answered 200 and never 409, which would invite the provider to retry forever; "
                + "202 means the message could not be correlated yet and is a private distinction, "
                + "since every provider reads any 2xx as delivered. This shared secret is not the "
                + "mechanism a real integration would use.")
            .Produces<ExternalCallbackResponse>(StatusCodes.Status200OK)
            .Produces<ExternalCallbackResponse>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return endpoints;
    }

    private static async Task<IResult> ReceiveAsync(
        string provider,
        string? secret,
        HttpContext context,
        ApplyExternalCallbackHandler handler,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorized(secret, context))
        {
            return CreateProblem(
                StatusCodes.Status401Unauthorized,
                "CALLBACK_UNAUTHORIZED",
                "The callback was not accompanied by a valid shared secret.");
        }

        // Checked after the secret, so an unauthenticated caller cannot learn which providers this
        // deployment speaks to by watching 404 turn into 200.
        if (!ExternalEvaluationWireNames.TryParseProvider(provider, out var parsed))
        {
            return CreateProblem(
                StatusCodes.Status404NotFound,
                "PROVIDER_NOT_REGISTERED",
                $"'{provider}' is not an external provider this API knows about.");
        }

        var (payload, failure) = await ReadPayloadAsync(context, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        if (!TryBuildMessage(payload!, out var message, out var rejection))
        {
            return rejection!;
        }

        try
        {
            var outcome = await handler.HandleAsync(parsed, message!, cancellationToken);
            var response = new ExternalCallbackResponse(
                outcome.ReceiptStatus,
                outcome.IsReplay,
                outcome.ReplayCount,
                outcome.ExternalEvaluationId,
                outcome.EvaluationStatus);

            // 202 for a message that found nothing to correlate with. It is an internal
            // distinction: the provider is told the message was accepted either way, because it
            // was — the receipt is written, and late linking will apply it.
            return outcome.ReceiptStatus == CallbackReceiptWireNames.Unmatched
                ? TypedResults.Accepted((string?)null, response)
                : TypedResults.Ok(response);
        }
        catch (ExternalCallbackUnavailableException exception)
        {
            context.Response.Headers.RetryAfter = "1";

            return CreateProblem(
                StatusCodes.Status503ServiceUnavailable,
                "CALLBACK_UNAVAILABLE",
                exception.Message);
        }
    }

    /// <summary>
    /// Constant-time comparison of the presented secret. Fails closed on an absent or blank
    /// configured secret, and compares over bytes of equal length so that the comparison itself
    /// cannot leak the length.
    /// </summary>
    private static bool IsAuthorized(string? secret, HttpContext context)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            return false;
        }

        var presented = context.Request.Headers[SecretHeader].ToString();
        if (string.IsNullOrEmpty(presented))
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(secret)),
            SHA256.HashData(Encoding.UTF8.GetBytes(presented)));
    }

    private static async Task<(ExternalCallbackPayload? Payload, IResult? Failure)> ReadPayloadAsync(
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (context.Request.ContentLength > MaximumBodyBytes)
        {
            return (null, TooLarge());
        }

        // Kestrel enforces the cap while reading, so a body with no declared length is bounded too.
        var sizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (sizeFeature is { IsReadOnly: false })
        {
            sizeFeature.MaxRequestBodySize = MaximumBodyBytes;
        }

        try
        {
            var payload = await context.Request.ReadFromJsonAsync<ExternalCallbackPayload>(
                PayloadOptions,
                cancellationToken);

            return payload is null
                ? (null, CreateProblem(
                    StatusCodes.Status400BadRequest,
                    "INVALID_CALLBACK",
                    "The callback body must be a JSON object."))
                : (payload, null);
        }
        catch (BadHttpRequestException exception) when (exception.StatusCode == StatusCodes.Status413PayloadTooLarge)
        {
            return (null, TooLarge());
        }
        catch (BadHttpRequestException)
        {
            return (null, CreateProblem(
                StatusCodes.Status400BadRequest,
                "INVALID_CALLBACK",
                "The callback body could not be read as JSON."));
        }
        catch (JsonException)
        {
            return (null, CreateProblem(
                StatusCodes.Status400BadRequest,
                "INVALID_CALLBACK",
                "The callback body could not be read as JSON."));
        }
    }

    private static bool TryBuildMessage(
        ExternalCallbackPayload payload,
        out ExternalCallbackMessage? message,
        out IResult? rejection)
    {
        message = null;
        rejection = null;

        if (string.IsNullOrWhiteSpace(payload.ExternalEvaluationId)
            && string.IsNullOrWhiteSpace(payload.ReferenceId))
        {
            rejection = CreateProblem(
                StatusCodes.Status400BadRequest,
                "INVALID_CALLBACK",
                "A callback must carry externalEvaluationId, referenceId, or both.");

            return false;
        }

        if (payload.Status is null
            || !TryParseStatus(payload.Status, out var status))
        {
            rejection = CreateProblem(
                StatusCodes.Status400BadRequest,
                "INVALID_CALLBACK",
                "status must be PENDING, APPROVED, DENIED or ERROR.");

            return false;
        }

        if (payload.Score is < 0)
        {
            rejection = CreateProblem(
                StatusCodes.Status400BadRequest,
                "INVALID_CALLBACK",
                "score must not be negative.");

            return false;
        }

        message = new(
            payload.ExternalEvaluationId,
            payload.ReferenceId,
            status,
            payload.Score,
            payload.OccurredAt);

        return true;
    }

    private static bool TryParseStatus(string value, out ExternalEvaluationStatus status)
    {
        try
        {
            status = ExternalEvaluationWireNames.ParseStatus(value);

            return true;
        }
        catch (ArgumentException)
        {
            status = default;

            return false;
        }
    }

    private static IResult TooLarge()
    {
        return CreateProblem(
            StatusCodes.Status413PayloadTooLarge,
            "CALLBACK_TOO_LARGE",
            $"A callback body must not exceed {MaximumBodyBytes} bytes.");
    }

    private static IResult CreateProblem(int statusCode, string code, string detail)
    {
        return Results.Problem(
            statusCode: statusCode,
            title: "External callback rejected",
            detail: detail,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = code,
            });
    }
}

/// <summary>
/// The callback body, as this API reads it.
/// </summary>
/// <remarks>
/// Unknown members are ignored rather than refused, which is the opposite of what the order importer
/// does and deliberately so: an import that silently drops a column is losing data somebody meant to
/// send, while a provider that adds a field to its callbacks must not be able to stop this system
/// from receiving them.
/// </remarks>
/// <param name="OccurredAt">
/// When the provider decided. Part of the deduplication key, so a provider that omits it makes two
/// messages about the same verdict indistinguishable — which is harmless, because the second one
/// would be a no-op anyway.
/// </param>
public sealed record ExternalCallbackPayload(
    string? ExternalEvaluationId,
    string? ReferenceId,
    string? Status,
    int? Score,
    DateTimeOffset? OccurredAt);

/// <param name="ReceiptStatus">
/// What became of this message: <c>APPLIED</c>, <c>NO_OP</c>, <c>SUPERSEDED</c>, <c>CONFLICTING</c>
/// or <c>UNMATCHED</c>.
/// </param>
/// <param name="IsReplay">Whether this exact message had already been recorded.</param>
/// <param name="EvaluationStatus">The state of the evaluation afterwards, when one was correlated.</param>
public sealed record ExternalCallbackResponse(
    string ReceiptStatus,
    bool IsReplay,
    int ReplayCount,
    Guid? ExternalEvaluationId,
    string? EvaluationStatus);
