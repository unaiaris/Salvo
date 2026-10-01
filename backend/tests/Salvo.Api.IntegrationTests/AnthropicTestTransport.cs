using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Salvo.Api.IntegrationTests;

/// <summary>
/// Stands in for Anthropic's Messages API. Nothing in this suite reaches the network.
/// </summary>
/// <remarks>
/// <para>
/// Every shape it answers with is one the official documentation shows, read on 2026-10-01: the
/// success body of <c>/v1/messages</c>, the refusal with its <c>stop_details</c>, the error envelope
/// with <c>request_id</c>, and the tier spend cap with <c>enforced_spend_limit_reached</c>. The
/// request ids are the two that page prints. What this cannot verify is that the real API behaves
/// like this; that is <c>E11C</c>, and if it does not, the simulator is corrected first.
/// </para>
/// <para>
/// It counts and keeps every request it receives, headers and body included, which is what lets a
/// test assert exactly one call per attempt and read what left the process.
/// </para>
/// </remarks>
internal sealed class AnthropicTestTransport : HttpMessageHandler
{
    /// <summary>A key nobody can use, built here and nowhere else.</summary>
    public const string FictitiousKey = "sk-ant-ficticia-e11b-solo-para-tests-0000";

    public const string Model = "claude-sonnet-5-5";

    /// <summary>The request ids the errors and rate limits pages print.</summary>
    public const string RequestId = "req_011CSHoEeqs5C35K2UUqR7Fy";

    public const string OtherRequestId = "req_018EeWyXxfu5pfWkrYcMdjWG";

    private readonly Lock gate = new();
    private readonly List<CapturedRequest> requests = [];

    /// <summary>What the next request is answered with. Re-evaluated on every call.</summary>
    public Func<HttpResponseMessage> Answer { get; set; } = () => Success(DraftJson("El pedido superó el umbral.", ["amount_anomaly"]));

    /// <summary>When set, the request fails before any answer, as a broken network does.</summary>
    public Exception? Failure { get; set; }

    /// <summary>When set, the request waits until it is cancelled, as a provider that never answers.</summary>
    public bool Hang { get; set; }

    public IReadOnlyList<CapturedRequest> Requests
    {
        get
        {
            lock (gate)
            {
                return [.. requests];
            }
        }
    }

    public static HttpResponseMessage Success(string text, string stopReason = "end_turn", int thinkingTokens = 0)
    {
        return Json(HttpStatusCode.OK, $$"""
            {
              "id": "msg_01XFDUDYJgAACzvnptvVoYEL",
              "type": "message",
              "role": "assistant",
              "model": "{{Model}}",
              "content": [{ "type": "text", "text": {{System.Text.Json.JsonSerializer.Serialize(text)}} }],
              "stop_reason": "{{stopReason}}",
              "stop_sequence": null,
              "stop_details": null,
              "usage": {
                "input_tokens": 1480,
                "output_tokens": 212,
                "output_tokens_details": { "thinking_tokens": {{thinkingTokens}}, "non_thinking_tokens": 212 }
              }
            }
            """);
    }

    /// <summary>The documented refusal: a 200 with empty content and the category in <c>stop_details</c>.</summary>
    public static HttpResponseMessage Refusal(string? category)
    {
        var categoryJson = category is null ? "null" : $"\"{category}\"";
        var explanation = category is null ? "null" : "\"This request was declined because it could enable cyber harm.\"";

        return Json(HttpStatusCode.OK, $$"""
            {
              "id": "msg_01XFUDYJgAACzvnptvVoYEL",
              "type": "message",
              "role": "assistant",
              "model": "{{Model}}",
              "content": [],
              "stop_reason": "refusal",
              "stop_details": { "type": "refusal", "category": {{categoryJson}}, "explanation": {{explanation}} },
              "usage": { "input_tokens": 412, "output_tokens": 0 }
            }
            """);
    }

    /// <summary>The documented error envelope.</summary>
    public static HttpResponseMessage Error(
        HttpStatusCode status,
        string type,
        string message = "Something went wrong.",
        string? errorCode = null,
        bool retryAfter = false,
        string requestId = RequestId)
    {
        var details = errorCode is null ? string.Empty : $$""", "details": { "error_code": "{{errorCode}}" }""";
        var response = Json(status, $$"""
            {
              "type": "error",
              "error": { "type": "{{type}}", "message": {{System.Text.Json.JsonSerializer.Serialize(message)}}{{details}} },
              "request_id": "{{requestId}}"
            }
            """, requestId);
        if (retryAfter)
        {
            response.Headers.Add("retry-after", "7");
        }

        return response;
    }

    /// <summary>The JSON a model returns inside its text block under the schema.</summary>
    public static string DraftJson(string summary, IEnumerable<string> rules)
    {
        return System.Text.Json.JsonSerializer.Serialize(new { summary, referencedRules = rules });
    }

    public static HttpResponseMessage Json(HttpStatusCode status, string body, string requestId = RequestId)
    {
        var response = new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        response.Headers.Add("request-id", requestId);

        return response;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        var headers = request.Headers
            .Concat(request.Content?.Headers ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>())
            .ToDictionary(header => header.Key.ToLowerInvariant(), header => string.Join(",", header.Value));

        lock (gate)
        {
            requests.Add(new(request.Method, request.RequestUri, headers, body));
        }

        if (Failure is not null)
        {
            throw Failure;
        }

        if (Hang)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        return Answer();
    }

    internal sealed record CapturedRequest(
        HttpMethod Method,
        Uri? Uri,
        IReadOnlyDictionary<string, string> Headers,
        string Body);
}

/// <summary>
/// Keeps every log entry at every level, message and exception both, so a test can search them.
/// </summary>
internal sealed class RecordingLoggerProvider : ILoggerProvider
{
    private readonly Lock gate = new();
    private readonly List<RecordedLog> entries = [];

    public IReadOnlyList<RecordedLog> Entries
    {
        get
        {
            lock (gate)
            {
                return [.. entries];
            }
        }
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new RecordingLogger(this, categoryName);
    }

    public void Dispose()
    {
    }

    private void Add(RecordedLog entry)
    {
        lock (gate)
        {
            entries.Add(entry);
        }
    }

    internal sealed record RecordedLog(string Category, LogLevel Level, string Message, Exception? Exception);

    private sealed class RecordingLogger(RecordingLoggerProvider owner, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);

            owner.Add(new(category, logLevel, formatter(state, exception), exception));
        }
    }
}
