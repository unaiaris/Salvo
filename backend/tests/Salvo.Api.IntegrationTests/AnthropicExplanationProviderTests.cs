using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Salvo.Application.Explanations;
using Salvo.Domain.Explanations;
using Salvo.Domain.Risk;
using Salvo.Infrastructure.Explanations;

namespace Salvo.Api.IntegrationTests;

/// <summary>
/// The Anthropic adapter, against a simulated transport that answers with the documented shapes.
/// </summary>
/// <remarks>
/// <para>
/// One test per row of the failure taxonomy of the stage 11 design (D6), with the shapes and request
/// ids the official pages showed on 2026-10-01. Each asserts the outcome the adapter reports and what
/// it hands to <c>FailureDetail</c>; the code is the exchange's, and the end-to-end tests cover it.
/// </para>
/// <para>
/// What the provider writes reaches <c>FailureDetail</c> only from a closed list, or as
/// <c>unrecognized</c>; a request id only with the documented shape. Several tests below send a
/// value the list does not have, or a message, and assert it does not come out the other side.
/// </para>
/// </remarks>
public sealed class AnthropicExplanationProviderTests : IDisposable
{
    private readonly AnthropicTestTransport transport = new();
    private readonly RecordingLoggerProvider logs = new();
    private readonly ILoggerFactory loggerFactory;
    private readonly AnthropicExplanationProvider provider;

    public AnthropicExplanationProviderTests()
    {
        loggerFactory = LoggerFactory.Create(builder => builder
            .SetMinimumLevel(LogLevel.Trace)
            .AddProvider(logs));
        provider = new(
            transport,
            new AnthropicSettings(AnthropicTestTransport.FictitiousKey, AnthropicTestTransport.Model),
            loggerFactory.CreateLogger<AnthropicExplanationProvider>());
    }

    public void Dispose()
    {
        provider.Dispose();
        loggerFactory.Dispose();
        logs.Dispose();
    }

    // ------------------------------------------------------------------ the request

    /// <summary>
    /// What leaves the process: the model asked for, the budget, thinking turned off up front at
    /// medium effort, the constant schema, and nothing that would change the model's choices.
    /// </summary>
    [Fact]
    public async Task TheRequestCarriesTheDocumentedShapeAndNothingElse()
    {
        await provider.ExplainAsync(Input(), CancellationToken.None);

        var request = Assert.Single(transport.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(new Uri("https://api.anthropic.com/v1/messages"), request.Uri);
        Assert.Equal("2023-06-01", request.Headers["anthropic-version"]);
        Assert.Equal(AnthropicTestTransport.FictitiousKey, request.Headers["x-api-key"]);
        Assert.StartsWith("application/json", request.Headers["content-type"], StringComparison.Ordinal);

        using var body = JsonDocument.Parse(request.Body);
        var root = body.RootElement;

        Assert.Equal(AnthropicTestTransport.Model, root.GetProperty("model").GetString());
        Assert.Equal(1024, root.GetProperty("max_tokens").GetInt32());
        Assert.Equal("between_tools", root.GetProperty("thinking").GetProperty("type").GetString());
        Assert.Single(root.GetProperty("thinking").EnumerateObject());
        Assert.Equal("medium", root.GetProperty("output_config").GetProperty("effort").GetString());

        var format = root.GetProperty("output_config").GetProperty("format");
        Assert.Equal("json_schema", format.GetProperty("type").GetString());
        var schema = format.GetProperty("schema");
        Assert.False(schema.GetProperty("additionalProperties").GetBoolean());
        Assert.Equal(
            ["summary", "referencedRules"],
            schema.GetProperty("required").EnumerateArray().Select(item => item.GetString()));
        Assert.Equal(
            RiskRuleNames.CanonicalOrder,
            schema.GetProperty("properties").GetProperty("referencedRules").GetProperty("items")
                .GetProperty("enum").EnumerateArray().Select(item => item.GetString()));

        var message = Assert.Single(root.GetProperty("messages").EnumerateArray());
        Assert.Equal("user", message.GetProperty("role").GetString());
        Assert.Equal(AnthropicFactSheet.Render(Input()), message.GetProperty("content").GetString());
        Assert.Equal(AnthropicFactSheet.Prompt(ExplanationLanguage.Spanish), root.GetProperty("system").GetString());

        // No sampling, no fallback to another model, no streaming, no tools.
        foreach (var absent in new[] { "temperature", "top_p", "top_k", "fallbacks", "stream", "tools", "tool_choice" })
        {
            Assert.False(root.TryGetProperty(absent, out _), $"The request carries '{absent}'.");
        }
    }

    /// <summary>
    /// The schema is one and the same for every evaluation, whatever rules fired: a different schema
    /// would compile a different grammar, and the first compilation is the slow one.
    /// </summary>
    [Fact]
    public async Task TheSchemaIsTheSameWhateverFired()
    {
        await provider.ExplainAsync(Input(), CancellationToken.None);
        await provider.ExplainAsync(
            Input() with { Signals = [RiskSignal.ForeignCountry(20, "AR", "BR", 3, 3, 100m)] },
            CancellationToken.None);

        var schemas = transport.Requests
            .Select(request => JsonDocument.Parse(request.Body).RootElement
                .GetProperty("output_config").GetProperty("format").GetProperty("schema").GetRawText())
            .ToArray();

        Assert.Equal(2, schemas.Length);
        Assert.Equal(schemas[0], schemas[1]);
    }

    /// <summary>
    /// Exactly one request per attempt, whatever comes back (decision 75). The retries are the
    /// row's: three, each visible and counted. An overload is the case a retrying client would
    /// retry, so it is the one counted here.
    /// </summary>
    [Fact]
    public async Task OneAttemptIsExactlyOneRequest()
    {
        transport.Answer = () => AnthropicTestTransport.Error((HttpStatusCode)529, "overloaded_error", "Overloaded");

        var result = await provider.ExplainAsync(Input(), CancellationToken.None);

        Assert.Equal(ExplanationProviderOutcome.Unavailable, result.Outcome);
        Assert.Single(transport.Requests);
    }

    // ------------------------------------------------------------------ a success status

    [Fact]
    public async Task AValidAnswerIsADraftWithTheModelAndTheTokensOfTheResponse()
    {
        transport.Answer = () => AnthropicTestTransport.Success(
            AnthropicTestTransport.DraftJson("El pedido obtuvo 90 puntos.", ["amount_anomaly", "foreign_country"]));

        var result = await provider.ExplainAsync(Input(), CancellationToken.None);

        Assert.Equal(ExplanationProviderOutcome.Drafted, result.Outcome);
        Assert.NotNull(result.Draft);
        Assert.Equal("El pedido obtuvo 90 puntos.", result.Draft.Summary);
        Assert.Equal(["amount_anomaly", "foreign_country"], result.Draft.ReferencedRules);
        Assert.Equal(AnthropicTestTransport.Model, result.ProviderVersion);
        Assert.Equal(1480, result.InputTokens);
        Assert.Equal(212, result.OutputTokens);
        Assert.Null(result.Diagnostic);
    }

    /// <summary>
    /// Structured output does not guarantee the capitalization of an enum value, and the verifier
    /// compares ordinally: the adapter normalizes before handing the draft over.
    /// </summary>
    [Fact]
    public async Task ARuleWithAnotherCapitalizationIsNormalized()
    {
        transport.Answer = () => AnthropicTestTransport.Success(
            AnthropicTestTransport.DraftJson("El pedido superó el umbral.", ["Amount_Anomaly", "FOREIGN_COUNTRY"]));

        var result = await provider.ExplainAsync(Input(), CancellationToken.None);

        Assert.Equal(ExplanationProviderOutcome.Drafted, result.Outcome);
        Assert.Equal(["amount_anomaly", "foreign_country"], result.Draft?.ReferencedRules);
    }

    /// <summary>A rule outside the enum the request sent makes the answer malformed, and is not copied.</summary>
    [Fact]
    public async Task ARuleOutsideTheEnumIsMalformedAndNotCopied()
    {
        transport.Answer = () => AnthropicTestTransport.Success(
            AnthropicTestTransport.DraftJson("El pedido superó el umbral.", ["regla_inventada_por_el_modelo"]));

        var result = await provider.ExplainAsync(Input(), CancellationToken.None);

        Assert.Equal(ExplanationProviderOutcome.Malformed, result.Outcome);
        Assert.Equal($"unknown_rule; {AnthropicTestTransport.RequestId}", result.Diagnostic);
    }

    [Fact]
    public async Task ARefusalWithACategoryIsRefusedWithThatCategory()
    {
        transport.Answer = () => AnthropicTestTransport.Refusal("cyber");

        var result = await provider.ExplainAsync(Input(), CancellationToken.None);

        Assert.Equal(ExplanationProviderOutcome.Refused, result.Outcome);
        Assert.Null(result.Draft);
        Assert.Equal($"cyber; {AnthropicTestTransport.RequestId}", result.Diagnostic);
        Assert.Equal(412, result.InputTokens);
        Assert.Equal(0, result.OutputTokens);
    }

    /// <summary>A null category is a documented, permanent value, not a placeholder.</summary>
    [Fact]
    public async Task ARefusalWithoutACategoryIsStillRefused()
    {
        transport.Answer = () => AnthropicTestTransport.Refusal(null);

        var result = await provider.ExplainAsync(Input(), CancellationToken.None);

        Assert.Equal(ExplanationProviderOutcome.Refused, result.Outcome);
        Assert.Equal(AnthropicTestTransport.RequestId, result.Diagnostic);
    }

    [Fact]
    public async Task ARefusalCategoryOffTheListIsUnrecognized()
    {
        transport.Answer = () => AnthropicTestTransport.Refusal("una_categoria_nueva");

        var result = await provider.ExplainAsync(Input(), CancellationToken.None);

        Assert.Equal(ExplanationProviderOutcome.Refused, result.Outcome);
        Assert.Equal($"unrecognized; {AnthropicTestTransport.RequestId}", result.Diagnostic);
    }

    /// <summary>
    /// A text cut at the budget, or at the context window, is an answer that is not a draft — not
    /// an unavailable provider. The stop reason is read before the content, which here is a half
    /// object that would not parse.
    /// </summary>
    [Theory]
    [InlineData("max_tokens")]
    [InlineData("model_context_window_exceeded")]
    [InlineData("stop_sequence")]
    [InlineData("pause_turn")]
    [InlineData("tool_use")]
    public async Task AnyStopReasonButTheEndOfTheTurnIsMalformedAndNamed(string stopReason)
    {
        transport.Answer = () => AnthropicTestTransport.Success("{\"summary\": \"El pedido obtuvo", stopReason);

        var result = await provider.ExplainAsync(Input(), CancellationToken.None);

        Assert.Equal(ExplanationProviderOutcome.Malformed, result.Outcome);
        Assert.Equal($"{stopReason}; {AnthropicTestTransport.RequestId}", result.Diagnostic);
        Assert.Equal(1480, result.InputTokens);
    }

    [Fact]
    public async Task AStopReasonOffTheListIsUnrecognized()
    {
        transport.Answer = () => AnthropicTestTransport.Success("{}", "una_razon_nueva");

        var result = await provider.ExplainAsync(Input(), CancellationToken.None);

        Assert.Equal(ExplanationProviderOutcome.Malformed, result.Outcome);
        Assert.Equal($"unrecognized; {AnthropicTestTransport.RequestId}", result.Diagnostic);
    }

    [Fact]
    public async Task ABodyThatIsNotJsonIsUnreadable()
    {
        transport.Answer = () => AnthropicTestTransport.Json(HttpStatusCode.OK, "<html>no es una respuesta</html>");

        var result = await provider.ExplainAsync(Input(), CancellationToken.None);

        Assert.Equal(ExplanationProviderOutcome.Malformed, result.Outcome);
        Assert.Equal($"unreadable; {AnthropicTestTransport.RequestId}", result.Diagnostic);
    }

    [Fact]
    public async Task AFinishedTurnWhoseTextIsNotTheSchemaIsUnreadable()
    {
        transport.Answer = () => AnthropicTestTransport.Success("El pedido obtuvo 90 puntos, sin JSON.");

        var result = await provider.ExplainAsync(Input(), CancellationToken.None);

        Assert.Equal(ExplanationProviderOutcome.Malformed, result.Outcome);
        Assert.Equal($"unreadable; {AnthropicTestTransport.RequestId}", result.Diagnostic);
    }

    /// <summary>
    /// <c>FailureDetail</c> never carries a word the model wrote, whatever the outcome. The model
    /// writes in three places a failed answer can reach — the text block, the refusal explanation,
    /// and the rule names — and none of them comes out.
    /// </summary>
    [Fact]
    public async Task TheDiagnosticNeverCarriesTextTheModelWrote()
    {
        const string ModelText = "TEXTO-DEL-MODELO-QUE-NO-DEBE-SALIR";
        var answers = new Func<HttpResponseMessage>[]
        {
            () => AnthropicTestTransport.Success($"{{\"summary\": \"{ModelText}", "max_tokens"),
            () => AnthropicTestTransport.Success(ModelText),
            () => AnthropicTestTransport.Success(AnthropicTestTransport.DraftJson(ModelText, [ModelText])),
            () => AnthropicTestTransport.Json(HttpStatusCode.OK, $$$"""
                {"model":"{{{AnthropicTestTransport.Model}}}","content":[{"type":"text","text":"{{{ModelText}}}"}],
                 "stop_reason":"refusal","stop_details":{"type":"refusal","category":"{{{ModelText}}}","explanation":"{{{ModelText}}}"},
                 "usage":{"input_tokens":1,"output_tokens":1}}
                """),
            () => AnthropicTestTransport.Error(HttpStatusCode.BadRequest, ModelText, ModelText, ModelText),
        };

        foreach (var answer in answers)
        {
            transport.Answer = answer;

            var result = await provider.ExplainAsync(Input(), CancellationToken.None);

            Assert.NotEqual(ExplanationProviderOutcome.Drafted, result.Outcome);
            Assert.DoesNotContain(ModelText, result.Diagnostic ?? string.Empty, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Thinking was asked to be off. A response that reports thinking tokens anyway leaves a warning
    /// with the request id and the figure, because it means the API changed and the budget and the
    /// cost estimate no longer hold. It is a log and not <c>FailureDetail</c>: on an accepted text the
    /// detail is emptied.
    /// </summary>
    [Fact]
    public async Task ThinkingTokensReportedAnywayLeaveAWarning()
    {
        transport.Answer = () => AnthropicTestTransport.Success(
            AnthropicTestTransport.DraftJson("El pedido superó el umbral.", ["amount_anomaly"]),
            thinkingTokens: 96);

        var result = await provider.ExplainAsync(Input(), CancellationToken.None);

        Assert.Equal(ExplanationProviderOutcome.Drafted, result.Outcome);
        var warning = Assert.Single(logs.Entries, entry => entry.Level == LogLevel.Warning);
        Assert.Contains("96", warning.Message, StringComparison.Ordinal);
        Assert.Contains(AnthropicTestTransport.RequestId, warning.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ZeroThinkingTokensLeaveNoWarning()
    {
        await provider.ExplainAsync(Input(), CancellationToken.None);

        Assert.DoesNotContain(logs.Entries, entry => entry.Level >= LogLevel.Warning);
    }

    // ------------------------------------------------------------------ an error status

    /// <summary>
    /// A rate limit with <c>retry-after</c> is transient: unavailable, with its type and request id.
    /// </summary>
    [Fact]
    public async Task ARateLimitWithRetryAfterIsUnavailable()
    {
        transport.Answer = () => AnthropicTestTransport.Error(
            HttpStatusCode.TooManyRequests,
            "rate_limit_error",
            "Number of request tokens has exceeded your per-minute rate limit",
            retryAfter: true);

        var result = await provider.ExplainAsync(Input(), CancellationToken.None);

        Assert.Equal(ExplanationProviderOutcome.Unavailable, result.Outcome);
        Assert.Equal($"HTTP 429; rate_limit_error; {AnthropicTestTransport.RequestId}", result.Diagnostic);
    }

    /// <summary>
    /// The tier spend cap, exactly as the rate limits page prints it: a 429 with no
    /// <c>retry-after</c> and <c>enforced_spend_limit_reached</c>. It keeps failing until a limit is
    /// raised, so it is named, and the message — which carries a date — is never read.
    /// </summary>
    [Fact]
    public async Task TheTierSpendCapIsNamedAsSuch()
    {
        transport.Answer = () => AnthropicTestTransport.Error(
            HttpStatusCode.TooManyRequests,
            "rate_limit_error",
            "You have reached your API usage limits: your organization has crossed its monthly API usage threshold, set based on your organization's API tier. You will regain access on 2026-09-01 at 00:00 UTC.",
            AnthropicExplanationProvider.SpendLimitReached,
            requestId: AnthropicTestTransport.OtherRequestId);

        var result = await provider.ExplainAsync(Input(), CancellationToken.None);

        Assert.Equal(ExplanationProviderOutcome.Unavailable, result.Outcome);
        Assert.Equal(
            $"HTTP 429; spend_cap; rate_limit_error; enforced_spend_limit_reached; {AnthropicTestTransport.OtherRequestId}",
            result.Diagnostic);
    }

    /// <summary>A 429 without <c>retry-after</c> is not transient, even without the error code.</summary>
    [Fact]
    public async Task ARateLimitWithoutRetryAfterIsASpendCap()
    {
        transport.Answer = () => AnthropicTestTransport.Error(HttpStatusCode.TooManyRequests, "rate_limit_error");

        var result = await provider.ExplainAsync(Input(), CancellationToken.None);

        Assert.Equal($"HTTP 429; spend_cap; rate_limit_error; {AnthropicTestTransport.RequestId}", result.Diagnostic);
    }

    /// <summary>
    /// A limit of one's own is a 400 of the same type as a malformed request. Without the message
    /// the two cannot be told apart, and the message is never read: the request id is what lets the
    /// Console tell.
    /// </summary>
    [Fact]
    public async Task AnOwnSpendLimitIsAnUnavailable400WithItsRequestId()
    {
        transport.Answer = () => AnthropicTestTransport.Error(
            HttpStatusCode.BadRequest,
            "invalid_request_error",
            "You have reached your specified workspace API usage limits. You will regain access on 2026-11-01 at 00:00 UTC.");

        var result = await provider.ExplainAsync(Input(), CancellationToken.None);

        Assert.Equal(ExplanationProviderOutcome.Unavailable, result.Outcome);
        Assert.Equal($"HTTP 400; invalid_request_error; {AnthropicTestTransport.RequestId}", result.Diagnostic);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "authentication_error")]
    [InlineData(HttpStatusCode.Forbidden, "permission_error")]
    public async Task CredentialsAreNamedAsSuch(HttpStatusCode status, string type)
    {
        transport.Answer = () => AnthropicTestTransport.Error(status, type, "invalid x-api-key");

        var result = await provider.ExplainAsync(Input(), CancellationToken.None);

        Assert.Equal(ExplanationProviderOutcome.Unavailable, result.Outcome);
        Assert.Equal(
            $"HTTP {(int)status}; credentials; {type}; {AnthropicTestTransport.RequestId}",
            result.Diagnostic);
    }

    [Theory]
    [InlineData(500, "api_error")]
    [InlineData(504, "timeout_error")]
    [InlineData(529, "overloaded_error")]
    [InlineData(402, "billing_error")]
    [InlineData(404, "not_found_error")]
    [InlineData(409, "conflict_error")]
    [InlineData(413, "request_too_large")]
    public async Task EveryOtherErrorStatusIsUnavailableWithItsTypeAndRequestId(int status, string type)
    {
        transport.Answer = () => AnthropicTestTransport.Error((HttpStatusCode)status, type);

        var result = await provider.ExplainAsync(Input(), CancellationToken.None);

        Assert.Equal(ExplanationProviderOutcome.Unavailable, result.Outcome);
        Assert.Equal($"HTTP {status}; {type}; {AnthropicTestTransport.RequestId}", result.Diagnostic);
    }

    /// <summary>An error type the list does not have is named as unrecognized, never copied.</summary>
    [Fact]
    public async Task AnErrorTypeOffTheListIsUnrecognized()
    {
        transport.Answer = () => AnthropicTestTransport.Error(
            HttpStatusCode.BadRequest,
            "un_error_que_la_documentacion_no_tenia",
            errorCode: "un_codigo_nuevo");

        var result = await provider.ExplainAsync(Input(), CancellationToken.None);

        Assert.Equal(
            $"HTTP 400; unrecognized; unrecognized; {AnthropicTestTransport.RequestId}",
            result.Diagnostic);
    }

    /// <summary>A request id that does not have the documented shape is left out, not copied.</summary>
    [Fact]
    public async Task ARequestIdWithAnotherShapeIsLeftOut()
    {
        transport.Answer = () => AnthropicTestTransport.Error(
            HttpStatusCode.InternalServerError,
            "api_error",
            requestId: "req_<script>alert(1)</script>");

        var result = await provider.ExplainAsync(Input(), CancellationToken.None);

        Assert.Equal("HTTP 500; api_error", result.Diagnostic);
    }

    /// <summary>An error status leaves a warning with the status and the classification, and nothing else.</summary>
    [Fact]
    public async Task AnErrorStatusLeavesAWarningWithoutTheBody()
    {
        transport.Answer = () => AnthropicTestTransport.Error(HttpStatusCode.TooManyRequests, "rate_limit_error", "MENSAJE-DEL-PROVEEDOR");

        await provider.ExplainAsync(Input(), CancellationToken.None);

        var warning = Assert.Single(logs.Entries, entry => entry.Level == LogLevel.Warning);
        Assert.Contains("429", warning.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("MENSAJE-DEL-PROVEEDOR", warning.Message, StringComparison.Ordinal);
        Assert.Null(warning.Exception);
    }

    /// <summary>A network that fails before any answer is unavailable, with no exception escaping.</summary>
    [Fact]
    public async Task ANetworkFailureIsUnavailable()
    {
        transport.Failure = new HttpRequestException("Connection refused (api.anthropic.com:443)");

        var result = await provider.ExplainAsync(Input(), CancellationToken.None);

        Assert.Equal(ExplanationProviderOutcome.Unavailable, result.Outcome);
        Assert.Equal("network", result.Diagnostic);
        Assert.Null(result.InputTokens);
    }

    /// <summary>
    /// A cancelled call is not this adapter's to classify: the cancellation propagates, and the port
    /// names it a timeout or a caller that went away, as it does for every provider.
    /// </summary>
    [Fact]
    public async Task ACancelledCallPropagatesTheCancellation()
    {
        transport.Hang = true;
        using var source = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => provider.ExplainAsync(Input(), source.Token));
    }

    /// <summary>
    /// <c>ORD_000011</c> of the demo corpus, the evaluation every golden text and fact test of the
    /// project is built around.
    /// </summary>
    internal static ExplanationInput Input()
    {
        RiskSignal[] signals =
        [
            RiskSignal.AmountAnomaly(40, 50786, "BRL", 50786m / 14937, AmountMedianScope.Merchant, 14937, 3, 90),
            RiskSignal.NewBuyerHighValue(30, 50786, "BRL", 14937, 3, 50786m / 14937),
            RiskSignal.ForeignCountry(20, "AR", "BR", 3, 3, 100m),
        ];

        return new(
            90,
            "CRITICAL",
            RuleConfig.E3V2.Version,
            "e4-v1",
            ExplanationLanguage.Spanish,
            signals,
            50786,
            "BRL",
            "AR",
            null,
            new DateTimeOffset(2026, 5, 11, 9, 36, 0, TimeSpan.Zero));
    }
}
