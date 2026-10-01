using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Salvo.Application.Explanations;
using Salvo.Domain.Explanations;
using Salvo.Domain.Risk;

namespace Salvo.Infrastructure.Explanations;

/// <summary>
/// The writer of explanations that is a model: Anthropic's Messages API, called directly.
/// </summary>
/// <remarks>
/// <para>
/// <strong>It writes; it never decides, and nothing it writes is trusted.</strong> What comes back is
/// a draft, and the draft goes to the same verifier the template's goes to, in the use case, before
/// anything is stored. This class does not check a single figure, by design: a check inside an adapter
/// passes because somebody remembered.
/// </para>
/// <para>
/// <strong><see cref="HttpClient"/> directly, and no SDK</strong> (decision of the coordinator, D2 of
/// the stage 11 design). No new dependency in a project that pins and locks every package, no log
/// channel of somebody else's to audit for the key, and the fine classification of a spend cap needs
/// the raw response anyway. The cost is that the protocol is ours — the version header, the shape of
/// structured output, the stop reasons — and it is covered by tests over the documented shapes. One
/// long-lived client, owned by this singleton, with no logging handler in front of it: there is
/// nothing to redact because nothing logs a request.
/// </para>
/// <para>
/// <strong>One request per attempt, and never a retry</strong> (decision 75). The retries are the
/// row's: three, each visible and counted.
/// </para>
/// <para>
/// <strong>What the provider writes never crosses as it is.</strong> An error type, an error code, a
/// stop reason and a refusal category reach <c>FailureDetail</c> only if they are in a closed list
/// compiled from the documentation, and as <c>unrecognized</c> otherwise; a request id only with the
/// documented shape. The <c>message</c> of an error is never read: it is prose, and it can change.
/// </para>
/// <para>
/// Thinking is turned off up front with <c>between_tools</c> at <c>medium</c> effort (decision 81):
/// Sonnet 5.5 thinks by default, <c>disabled</c> is refused, and thinking counts against
/// <see cref="MaxTokens"/> and is billed as output. A response that reports thinking tokens anyway
/// leaves a warning, because it means the API changed under this class.
/// </para>
/// </remarks>
public sealed partial class AnthropicExplanationProvider : IExplanationProvider, IDisposable
{
    /// <summary>
    /// The output budget. A summary of at most 1,200 characters is a few hundred tokens in either
    /// language, so a text that runs long reaches the verifier and fails as <c>TOO_LONG</c> rather
    /// than being cut here and failing as <c>MALFORMED_OUTPUT</c>.
    /// </summary>
    public const int MaxTokens = 1024;

    /// <summary>The <c>anthropic-version</c> header, which every request must carry.</summary>
    public const string ApiVersion = "2023-06-01";

    /// <summary>The lowest thinking setting Sonnet 5.5 accepts.</summary>
    public const string ThinkingType = "between_tools";

    /// <summary>The effort, at or below which <see cref="ThinkingType"/> is accepted.</summary>
    public const string Effort = "medium";

    /// <summary>
    /// What <c>FailureDetail</c> says for anything the provider wrote that is not on a closed list.
    /// </summary>
    public const string Unrecognized = "unrecognized";

    /// <summary>The documented error code that tells a tier spend cap from a rate limit.</summary>
    public const string SpendLimitReached = "enforced_spend_limit_reached";

    private const int MaximumResponseBytes = 1024 * 1024;

    /// <summary>
    /// The one schema every request carries. Constant on purpose (decision 81): every distinct schema
    /// compiles its own grammar the first time it is used, and the first time is the slow one. The
    /// <c>enum</c> is every rule of the engine; that the rule cited actually fired is the verifier's
    /// to check, as <c>NOT_GROUNDED_RULE</c>, which is where that guarantee already lives.
    /// </summary>
    private static readonly string Schema = new JsonObject
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["summary"] = new JsonObject { ["type"] = "string" },
            ["referencedRules"] = new JsonObject
            {
                ["type"] = "array",
                ["items"] = new JsonObject
                {
                    ["type"] = "string",
                    ["enum"] = new JsonArray([.. RiskRuleNames.CanonicalOrder.Select(rule => JsonValue.Create(rule))]),
                },
            },
        },
        ["required"] = new JsonArray("summary", "referencedRules"),
        ["additionalProperties"] = false,
    }.ToJsonString();

    /// <summary>The error types of the errors page, read on 2026-10-01.</summary>
    private static readonly HashSet<string> ErrorTypes = new(StringComparer.Ordinal)
    {
        "invalid_request_error",
        "authentication_error",
        "billing_error",
        "permission_error",
        "not_found_error",
        "conflict_error",
        "request_too_large",
        "rate_limit_error",
        "api_error",
        "timeout_error",
        "overloaded_error",
    };

    /// <summary>The error codes of the rate limits page, read on 2026-10-01.</summary>
    private static readonly HashSet<string> ErrorCodes = new(StringComparer.Ordinal) { SpendLimitReached };

    /// <summary>The stop reasons of the stop reasons page, read on 2026-10-01.</summary>
    private static readonly HashSet<string> StopReasons = new(StringComparer.Ordinal)
    {
        "end_turn",
        "max_tokens",
        "stop_sequence",
        "tool_use",
        "pause_turn",
        "refusal",
        "model_context_window_exceeded",
    };

    /// <summary>The refusal categories of Sonnet 5.5, read on 2026-10-01.</summary>
    private static readonly HashSet<string> RefusalCategories = new(StringComparer.Ordinal)
    {
        "cyber",
        "bio",
        "frontier_llm",
        "reasoning_extraction",
        "general_harms",
    };

    private readonly HttpClient http;
    private readonly AnthropicSettings settings;
    private readonly ILogger<AnthropicExplanationProvider> logger;

    /// <param name="handler">
    /// The transport, owned from here on. In a deployment it is a pooled socket handler; in a test,
    /// a simulated one, which is the only way this class is ever exercised without a key.
    /// </param>
    public AnthropicExplanationProvider(
        HttpMessageHandler handler,
        AnthropicSettings settings,
        ILogger<AnthropicExplanationProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(logger);

        // No timeout of its own: the port gives every call its budget and cancels the token, and two
        // timeouts would disagree about which one fired.
        http = new HttpClient(handler, disposeHandler: true)
        {
            Timeout = Timeout.InfiniteTimeSpan,
            MaxResponseContentBufferSize = MaximumResponseBytes,
        };
        this.settings = settings;
        this.logger = logger;
    }

    /// <summary>The endpoint of the Messages API.</summary>
    public static Uri MessagesEndpoint { get; } = new("https://api.anthropic.com/v1/messages");

    public ExplanationProvider Provider => ExplanationProvider.Anthropic;

    public string TemplateVersion => AnthropicFactSheet.PromptVersion;

    public async Task<ExplanationProviderResult> ExplainAsync(
        ExplanationInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);

        using var request = new HttpRequestMessage(HttpMethod.Post, MessagesEndpoint)
        {
            Content = new StringContent(Body(input), Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("x-api-key", settings.ApiKey);
        request.Headers.Add("anthropic-version", ApiVersion);

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException exception) when (!cancellationToken.IsCancellationRequested)
        {
            Log.Unreachable(logger, exception.GetType().Name);

            return ExplanationProviderResult.Unavailable("network");
        }

        using (response)
        {
            var requestId = RequestIdOf(response);

            string body;
            try
            {
                body = await response.Content.ReadAsStringAsync(cancellationToken);
            }
            catch (Exception exception) when (exception is HttpRequestException or IOException
                && !cancellationToken.IsCancellationRequested)
            {
                Log.Unreachable(logger, exception.GetType().Name);

                return ExplanationProviderResult.Unavailable(Join("network", requestId));
            }

            return response.IsSuccessStatusCode
                ? Answered(body, requestId)
                : Failed(response, body, requestId);
        }
    }

    public void Dispose()
    {
        http.Dispose();
    }

    /// <summary>
    /// The request: the model asked for, the budget, thinking turned off up front, the constant
    /// schema, the prompt and the sheet of facts. Nothing else travels — no sampling parameters, no
    /// fallbacks: a refusal here is an outcome to record, not something to hide behind another model.
    /// </summary>
    private string Body(ExplanationInput input)
    {
        return new JsonObject
        {
            ["model"] = settings.Model,
            ["max_tokens"] = MaxTokens,
            ["thinking"] = new JsonObject { ["type"] = ThinkingType },
            ["output_config"] = new JsonObject
            {
                ["effort"] = Effort,
                ["format"] = new JsonObject
                {
                    ["type"] = "json_schema",
                    ["schema"] = JsonNode.Parse(Schema),
                },
            },
            ["system"] = AnthropicFactSheet.Prompt(input.Language),
            ["messages"] = new JsonArray(new JsonObject
            {
                ["role"] = "user",
                ["content"] = AnthropicFactSheet.Render(input),
            }),
        }.ToJsonString();
    }

    /// <summary>
    /// An answer with a success status. The stop reason is read <em>before</em> the content: a
    /// refusal can come back without any JSON, and a text cut at the budget is not the schema.
    /// </summary>
    private ExplanationProviderResult Answered(string body, string? requestId)
    {
        JsonElement root;
        try
        {
            using var document = JsonDocument.Parse(body);
            root = document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return ExplanationProviderResult.Malformed(Join("unreadable", requestId));
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            return ExplanationProviderResult.Malformed(Join("unreadable", requestId));
        }

        var (inputTokens, outputTokens, thinkingTokens) = UsageOf(root);
        if (thinkingTokens > 0)
        {
            Log.ThinkingReported(logger, thinkingTokens.Value, requestId ?? Unrecognized);
        }

        var version = ModelOf(root);
        var stopReason = StringOf(root, "stop_reason");

        if (stopReason == "refusal")
        {
            var category = root.TryGetProperty("stop_details", out var details)
                && details.ValueKind == JsonValueKind.Object
                ? StringOf(details, "category")
                : null;

            return ExplanationProviderResult.Refused(
                Join(category is null ? null : Closed(RefusalCategories, category), requestId),
                version,
                inputTokens,
                outputTokens);
        }

        if (stopReason != "end_turn")
        {
            return ExplanationProviderResult.Malformed(
                Join(stopReason is null ? Unrecognized : Closed(StopReasons, stopReason), requestId),
                version,
                inputTokens,
                outputTokens);
        }

        var (draft, problem) = DraftOf(root);

        return draft is null
            ? ExplanationProviderResult.Malformed(Join(problem, requestId), version, inputTokens, outputTokens)
            : ExplanationProviderResult.Drafted(draft, version, inputTokens, outputTokens);
    }

    /// <summary>
    /// An answer with an error status: always unavailable, and classified for whoever reads
    /// <c>FailureDetail</c>. The two cases worth naming are the ones somebody has to act on: a spend
    /// cap, which keeps failing until a limit is raised, and credentials.
    /// </summary>
    /// <remarks>
    /// A tier spend cap is a 429 without <c>retry-after</c>, or with the documented error code; a
    /// limit of one's own is a 400 of the same type as a malformed request, and cannot be told apart
    /// without reading the message, which this never reads. The request id lets the Console tell.
    /// </remarks>
    private ExplanationProviderResult Failed(HttpResponseMessage response, string body, string? requestId)
    {
        var status = (int)response.StatusCode;
        var (type, code) = ErrorOf(body);
        var retryAfter = response.Headers.RetryAfter is not null || response.Headers.Contains("retry-after");

        var kind = response.StatusCode switch
        {
            HttpStatusCode.TooManyRequests when !retryAfter || code == SpendLimitReached => "spend_cap",
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "credentials",
            _ => null,
        };
        var closedType = type is null ? Unrecognized : Closed(ErrorTypes, type);

        Log.ErrorStatus(logger, status, kind ?? "-", closedType);

        return ExplanationProviderResult.Unavailable(Join(
            $"HTTP {status.ToString(CultureInfo.InvariantCulture)}",
            kind,
            closedType,
            code is null ? null : Closed(ErrorCodes, code),
            requestId));
    }

    /// <summary>
    /// The draft, from the first text block, which structured output guarantees to be the schema —
    /// and which is checked anyway, because a guarantee is a promise about the other side.
    /// </summary>
    /// <returns>The draft, or the word that names why there is none. Never a value the model wrote.</returns>
    private static (ExplanationDraft? Draft, string Problem) DraftOf(JsonElement root)
    {
        if (!root.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
        {
            return (null, "unreadable");
        }

        var text = content.EnumerateArray()
            .Where(block => block.ValueKind == JsonValueKind.Object && StringOf(block, "type") == "text")
            .Select(block => StringOf(block, "text"))
            .FirstOrDefault();
        if (text is null)
        {
            return (null, "unreadable");
        }

        JsonElement answer;
        try
        {
            using var document = JsonDocument.Parse(text);
            answer = document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return (null, "unreadable");
        }

        if (answer.ValueKind != JsonValueKind.Object
            || StringOf(answer, "summary") is not { } summary
            || !answer.TryGetProperty("referencedRules", out var cited)
            || cited.ValueKind != JsonValueKind.Array)
        {
            return (null, "unreadable");
        }

        var rules = new List<string>();
        foreach (var rule in cited.EnumerateArray())
        {
            if (rule.ValueKind != JsonValueKind.String || rule.GetString() is not { } value)
            {
                return (null, "unreadable");
            }

            // Structured output does not guarantee the capitalization of an enum value, and the
            // verifier compares ordinally. Normalized, then checked against the enum the request
            // sent; a value outside it is named by what it is, never copied.
            var normalized = value.ToLowerInvariant();
            if (!RiskRuleNames.CanonicalOrder.Contains(normalized, StringComparer.Ordinal))
            {
                return (null, "unknown_rule");
            }

            rules.Add(normalized);
        }

        return (new ExplanationDraft(summary, rules), string.Empty);
    }

    private static (int? Input, int? Output, int? Thinking) UsageOf(JsonElement root)
    {
        if (!root.TryGetProperty("usage", out var usage) || usage.ValueKind != JsonValueKind.Object)
        {
            return (null, null, null);
        }

        var thinking = usage.TryGetProperty("output_tokens_details", out var details)
            && details.ValueKind == JsonValueKind.Object
            ? IntegerOf(details, "thinking_tokens")
            : null;

        return (IntegerOf(usage, "input_tokens"), IntegerOf(usage, "output_tokens"), thinking);
    }

    private static (string? Type, string? Code) ErrorOf(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("error", out var error)
                || error.ValueKind != JsonValueKind.Object)
            {
                return (null, null);
            }

            var code = error.TryGetProperty("details", out var details)
                && details.ValueKind == JsonValueKind.Object
                ? StringOf(details, "error_code")
                : null;

            return (StringOf(error, "type"), code);
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }

    /// <summary>
    /// The model that answered, from the response. Shaped like a model id or not taken at all: it
    /// reaches the console, and it is the one string of the answer that does.
    /// </summary>
    private static string? ModelOf(JsonElement root)
    {
        return StringOf(root, "model") is { } model && ModelPattern().IsMatch(model) ? model : null;
    }

    private static string? RequestIdOf(HttpResponseMessage response)
    {
        return response.Headers.TryGetValues("request-id", out var values)
            && values.FirstOrDefault() is { } value
            && RequestIdPattern().IsMatch(value)
            ? value
            : null;
    }

    private static string Closed(HashSet<string> known, string value)
    {
        return known.Contains(value) ? value : Unrecognized;
    }

    private static string? StringOf(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static int? IntegerOf(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetInt32(out var number)
            && number >= 0
            ? number
            : null;
    }

    private static string? Join(params string?[] parts)
    {
        var present = parts.Where(part => !string.IsNullOrEmpty(part)).ToArray();

        return present.Length == 0 ? null : string.Join("; ", present);
    }

    /// <summary>The shape of the documented example, <c>req_018EeWyXxfu5pfWkrYcMdjWG</c>, with a ceiling.</summary>
    [GeneratedRegex("^req_[A-Za-z0-9]{1,64}$", RegexOptions.CultureInvariant)]
    private static partial Regex RequestIdPattern();

    [GeneratedRegex("^[a-z0-9][a-z0-9.-]{0,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex ModelPattern();

    /// <summary>
    /// Every message this adapter logs. None takes a request, a response, a header or a body: the
    /// key travels in a header, and <see cref="HttpRequestMessage.ToString"/> writes the headers out.
    /// </summary>
    private static partial class Log
    {
        [LoggerMessage(
            Level = LogLevel.Warning,
            Message = "Anthropic answered HTTP {Status}, recorded as {Kind} / {ErrorType}.")]
        public static partial void ErrorStatus(ILogger logger, int status, string kind, string errorType);

        [LoggerMessage(
            Level = LogLevel.Warning,
            Message = "Anthropic could not be reached: {Failure}.")]
        public static partial void Unreachable(ILogger logger, string failure);

        [LoggerMessage(
            Level = LogLevel.Warning,
            Message = "Anthropic reported {ThinkingTokens} thinking tokens on request {RequestId}, which asked for none: the API changed under this adapter.")]
        public static partial void ThinkingReported(ILogger logger, int thinkingTokens, string requestId);
    }
}
