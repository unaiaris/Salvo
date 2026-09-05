using System.Data.Common;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Salvo.Api;
using Salvo.Application.Explanations;
using Salvo.Domain.Explanations;
using Salvo.Domain.Risk;

namespace Salvo.Api.IntegrationTests;

/// <summary>
/// Shared scaffolding for the explanation tests: how to ask for one, and the providers that stand
/// in for the ones this build does not have.
/// </summary>
internal static class ExplanationTestCorpus
{
    public static Task<HttpResponseMessage> RequestAsync(
        HttpClient client,
        Guid alertId,
        bool regenerate = false)
    {
        return client.PostAsJsonAsync(
            $"/api/alerts/{alertId}/explanation",
            new RequestExplanationRequest(regenerate));
    }

    public static async Task<RequestExplanationResult> RequestOkAsync(
        HttpClient client,
        Guid alertId,
        bool regenerate = false)
    {
        var response = await RequestAsync(client, alertId, regenerate);
        response.EnsureSuccessStatusCode();

        return Assert.IsType<RequestExplanationResult>(
            await response.Content.ReadFromJsonAsync<RequestExplanationResult>());
    }

    /// <summary>The <c>code</c> extension of a problem response.</summary>
    public static async Task<string?> ProblemCodeAsync(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return document.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    /// <summary>
    /// A provider whose behaviour a test switches between calls, so one alert can fail and then
    /// succeed on the same row.
    /// </summary>
    public sealed class SwitchableProvider : IExplanationProvider
    {
        public ExplanationProvider Provider => ExplanationProvider.Mock;

        public string TemplateVersion => "e7-v1";

        /// <summary>What the next call does.</summary>
        public ProviderBehaviour Behaviour { get; set; } = ProviderBehaviour.Succeed;

        public int Calls { get; private set; }

        /// <summary>The last input it was handed, for the tests that inspect the boundary.</summary>
        public ExplanationInput? LastInput { get; private set; }

        public Task<ExplanationDraft> ExplainAsync(
            ExplanationInput input,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(input);

            Calls++;
            LastInput = input;

            return Behaviour switch
            {
                ProviderBehaviour.Succeed => Task.FromResult(Succeed(input)),
                ProviderBehaviour.InventANumber => Task.FromResult(Invent(input)),
                ProviderBehaviour.CiteAnUnraisedRule => Task.FromResult(CiteUnraised(input)),
                ProviderBehaviour.Refuse => Task.FromResult(
                    new ExplanationDraft(null, [])),
                ProviderBehaviour.Throw => throw new InvalidOperationException("The provider is down."),
                _ => Task.FromResult(Succeed(input)),
            };
        }

        /// <summary>
        /// A summary with no figures at all, which is trivially grounded. Tests that are about the
        /// lifecycle rather than about the checking use it so the checking cannot be what fails.
        /// </summary>
        private static ExplanationDraft Succeed(ExplanationInput input)
        {
            return new(
                "El pedido superó el umbral por las reglas que se listan.",
                [.. input.Signals.Select(signal => signal.Rule)]);
        }

        /// <summary>
        /// A summary carrying a figure the evaluation cannot back. The figure is chosen at run time
        /// — the smallest positive integer no fact supports — rather than written into the test, so
        /// that it stays invented when the corpus changes.
        /// </summary>
        private static ExplanationDraft Invent(ExplanationInput input)
        {
            var facts = ExplanationFacts.For(
                input,
                SignalFacts.ParseAll(input.Signals),
                RuleConfig.E3V1);

            return new(
                $"El monto es {SmallestUngrounded(facts)} veces la mediana del comercio.",
                [.. input.Signals.Select(signal => signal.Rule)]);
        }

        private static ExplanationDraft CiteUnraised(ExplanationInput input)
        {
            var raised = input.Signals.Select(signal => signal.Rule).ToHashSet(StringComparer.Ordinal);
            var absent = RiskRuleNames.CanonicalOrder.First(rule => !raised.Contains(rule));

            return new("El pedido superó el umbral.", [absent]);
        }
    }

    /// <summary>
    /// The smallest positive integer the facts do not back, under the very rule the real check
    /// uses. A figure picked this way is invented by construction.
    /// </summary>
    public static int SmallestUngrounded(ExplanationFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);

        var candidate = 1;
        while (facts.IsGrounded(candidate))
        {
            candidate++;
        }

        return candidate;
    }

    public enum ProviderBehaviour
    {
        Succeed = 1,
        InventANumber = 2,
        CiteAnUnraisedRule = 3,
        Refuse = 4,
        Throw = 5,
    }

    /// <summary>
    /// A provider that cancels the caller's token from inside the call, which is what a browser
    /// closing looks like from here.
    /// </summary>
    public sealed class CallerCancellingProvider(CancellationTokenSource source) : IExplanationProvider
    {
        public ExplanationProvider Provider => ExplanationProvider.Mock;

        public string TemplateVersion => "e7-v1";

        public Task<ExplanationDraft> ExplainAsync(
            ExplanationInput input,
            CancellationToken cancellationToken)
        {
            source.Cancel();
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(new ExplanationDraft("nunca llega", []));
        }
    }

    /// <summary>
    /// Records the text of every command a host issues, so a test can say which tables a request
    /// wrote to instead of trusting that it wrote to the right one.
    /// </summary>
    public sealed class RecordingCommandInterceptor : DbCommandInterceptor
    {
        private readonly List<string> commands = [];
        private readonly Lock gate = new();

        public IReadOnlyList<string> Commands
        {
            get
            {
                lock (gate)
                {
                    return [.. commands];
                }
            }
        }

        public void Clear()
        {
            lock (gate)
            {
                commands.Clear();
            }
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            Record(command);

            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Record(command);

            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override InterceptionResult<int> NonQueryExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result)
        {
            Record(command);

            return base.NonQueryExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Record(command);

            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override InterceptionResult<object> ScalarExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<object> result)
        {
            Record(command);

            return base.ScalarExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<object> result,
            CancellationToken cancellationToken = default)
        {
            Record(command);

            return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
        }

        private void Record(DbCommand command)
        {
            lock (gate)
            {
                commands.Add(command.CommandText);
            }
        }
    }
}
