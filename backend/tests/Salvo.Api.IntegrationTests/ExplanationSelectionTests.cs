using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Salvo.Application.Alerts;
using Salvo.Application.Explanations;
using Salvo.Domain.Explanations;
using Salvo.Infrastructure.Persistence;

namespace Salvo.Api.IntegrationTests;

/// <summary>
/// Which explanation the console shows when an evaluation has rows of more than one writer, and what
/// the one button asks for in each case (decision 79).
/// </summary>
/// <remarks>
/// <para>
/// The current writer here is a model: a provider that declares itself <c>ANTHROPIC</c> /
/// <c>anthropic-p1</c> and answers without a network. The other writer is the template,
/// <c>MOCK</c> / <c>e7-v2</c>. Every row is written straight into the database, so each test states
/// its case exactly rather than reaching it through a sequence of requests.
/// </para>
/// <para>
/// <strong>The button is tested in two halves, each in its own layer.</strong> The frontend asserts
/// which request the button builds from the view of each case. These assert, over the same state, what
/// the API answers to exactly that request: <c>applied</c> and one attempt more where there is a
/// button, the conflict where there is none.
/// </para>
/// </remarks>
public sealed class ExplanationSelectionTests
{
    private const string Template = "e7-v2";
    private const string Prompt = "anthropic-p1";

    /// <summary>Case 1: the current writer has its text. That is what is shown, and there is nothing to ask.</summary>
    [Fact]
    public async Task Case1TheTextOfTheCurrentWriterIsShownAndNothingIsOffered()
    {
        await using var scenario = await Scenario.StartAsync();
        var ready = await scenario.InsertAsync(ExplanationProvider.Anthropic, Prompt, ExplanationStatus.Ready);

        var shown = await scenario.ShownAsync();

        Assert.Equal(ready, shown.Id);
        Assert.False(shown.WrittenByAnotherTemplate);
        Assert.Null(shown.CurrentWriterAttempt);

        var asked = await ExplanationTestCorpus.RequestAsync(scenario.Client, scenario.AlertId, regenerate: true);
        Assert.Equal(HttpStatusCode.Conflict, asked.StatusCode);
        Assert.Equal("EXPLANATION_ALREADY_READY", await ExplanationTestCorpus.ProblemCodeAsync(asked));
    }

    /// <summary>
    /// Case 2: the template wrote a correct paragraph, and the model's attempt failed after it. The
    /// paragraph stays on screen, with the failed attempt beside it — and the button retries that row.
    /// </summary>
    /// <remarks>
    /// The case the stage design exists for. Choosing the most recently requested row shows the failure
    /// and hides the paragraph that is still in the database; and asking without <c>regenerate</c>,
    /// which is what a button that only sees «written by another writer» would do, finds the failed
    /// row of the current writer and answers that nothing changed — the defect of <c>E7D</c> in the
    /// scenario a model creates.
    /// </remarks>
    [Fact]
    public async Task Case2AReadyTextOfTheTemplateIsNotHiddenBehindAFailedAttemptOfTheModel()
    {
        await using var scenario = await Scenario.StartAsync();
        var paragraph = await scenario.InsertAsync(ExplanationProvider.Mock, Template, ExplanationStatus.Ready, minutesAgo: 10);
        var attempt = await scenario.InsertAsync(ExplanationProvider.Anthropic, Prompt, ExplanationStatus.Failed, minutesAgo: 2);

        var shown = await scenario.ShownAsync();

        Assert.Equal(paragraph, shown.Id);
        Assert.Equal(ExplanationWireNames.Ready, shown.Status);
        Assert.True(shown.WrittenByAnotherTemplate);
        Assert.Equal(ExplanationWireNames.Anthropic, shown.CurrentWriterProvider);
        Assert.NotNull(shown.CurrentWriterAttempt);
        Assert.Equal(attempt, shown.CurrentWriterAttempt.Id);
        Assert.Equal(ExplanationWireNames.Failed, shown.CurrentWriterAttempt.Status);
        Assert.Equal(ExplanationWireNames.ProviderRefused, shown.CurrentWriterAttempt.FailureCode);
        Assert.Equal("cyber; req_011CSHoEeqs5C35K2UUqR7Fy", shown.CurrentWriterAttempt.FailureDetail);
        Assert.False(shown.CurrentWriterAttempt.AttemptsExhausted);

        // The button retries the attempt: the request «Volver a intentar» makes.
        var retried = await ExplanationTestCorpus.RequestOkAsync(scenario.Client, scenario.AlertId, regenerate: true);

        Assert.True(retried.Applied);
        Assert.Equal(attempt, retried.Explanation.Id);
        Assert.Equal(2, retried.Explanation.AttemptCount);
        Assert.Equal(ExplanationWireNames.Ready, retried.Explanation.Status);
        Assert.Equal(attempt, (await scenario.ShownAsync()).Id);
    }

    /// <summary>Case 2 with the budget spent: no button, and the attempt says why.</summary>
    [Fact]
    public async Task Case2WithTheAttemptsSpentOffersNothingAndSaysSo()
    {
        await using var scenario = await Scenario.StartAsync();
        await scenario.InsertAsync(ExplanationProvider.Mock, Template, ExplanationStatus.Ready, minutesAgo: 10);
        await scenario.InsertAsync(ExplanationProvider.Anthropic, Prompt, ExplanationStatus.Failed, minutesAgo: 2, attempts: 3);

        var shown = await scenario.ShownAsync();

        Assert.NotNull(shown.CurrentWriterAttempt);
        Assert.True(shown.CurrentWriterAttempt.AttemptsExhausted);
        Assert.Equal(ExplanationWireNames.AttemptLimitReached, shown.CurrentWriterAttempt.FailureCode);

        var asked = await ExplanationTestCorpus.RequestAsync(scenario.Client, scenario.AlertId, regenerate: true);
        Assert.Equal(HttpStatusCode.Conflict, asked.StatusCode);
        Assert.Equal("EXPLANATION_ATTEMPTS_EXHAUSTED", await ExplanationTestCorpus.ProblemCodeAsync(asked));
    }

    /// <summary>Case 2 with the current writer still answering: no button, the wait is shown.</summary>
    [Fact]
    public async Task Case2WithTheCurrentWriterPendingOffersNothing()
    {
        await using var scenario = await Scenario.StartAsync();
        await scenario.InsertAsync(ExplanationProvider.Mock, Template, ExplanationStatus.Ready, minutesAgo: 10);
        await scenario.InsertAsync(ExplanationProvider.Anthropic, Prompt, ExplanationStatus.Pending, minutesAgo: 0);

        var shown = await scenario.ShownAsync();

        Assert.Equal(ExplanationWireNames.Ready, shown.Status);
        Assert.Equal(ExplanationWireNames.Pending, shown.CurrentWriterAttempt?.Status);

        var asked = await ExplanationTestCorpus.RequestAsync(scenario.Client, scenario.AlertId, regenerate: true);
        Assert.Equal(HttpStatusCode.Conflict, asked.StatusCode);
        Assert.Equal("EXPLANATION_PENDING", await ExplanationTestCorpus.ProblemCodeAsync(asked));
    }

    /// <summary>
    /// Case 3: a text of another writer and no row of the current one — the case 1d of the smoke. The
    /// button creates the row of the current writer beside the old one, without regenerating anything.
    /// </summary>
    [Fact]
    public async Task Case3AnotherWritersTextWithNoRowOfTheCurrentWriterOffersToWriteIt()
    {
        await using var scenario = await Scenario.StartAsync();
        var paragraph = await scenario.InsertAsync(ExplanationProvider.Mock, Template, ExplanationStatus.Ready);

        var shown = await scenario.ShownAsync();

        Assert.Equal(paragraph, shown.Id);
        Assert.True(shown.WrittenByAnotherTemplate);
        Assert.Null(shown.CurrentWriterAttempt);

        var written = await ExplanationTestCorpus.RequestOkAsync(scenario.Client, scenario.AlertId);

        Assert.True(written.Applied);
        Assert.NotEqual(paragraph, written.Explanation.Id);
        Assert.Equal(1, written.Explanation.AttemptCount);
        Assert.Equal(ExplanationWireNames.Anthropic, written.Explanation.Provider);
        Assert.Equal(2, await scenario.CountAsync());
    }

    /// <summary>Case 4: nothing ready, and the current writer failed. Its row is shown, and retried.</summary>
    [Fact]
    public async Task Case4NothingReadyShowsTheRowOfTheCurrentWriterAndRetriesIt()
    {
        await using var scenario = await Scenario.StartAsync();
        var failed = await scenario.InsertAsync(ExplanationProvider.Anthropic, Prompt, ExplanationStatus.Failed);

        var shown = await scenario.ShownAsync();

        Assert.Equal(failed, shown.Id);
        Assert.False(shown.WrittenByAnotherTemplate);
        Assert.Null(shown.CurrentWriterAttempt);
        Assert.Equal("cyber; req_011CSHoEeqs5C35K2UUqR7Fy", shown.FailureDetail);

        var retried = await ExplanationTestCorpus.RequestOkAsync(scenario.Client, scenario.AlertId, regenerate: true);

        Assert.True(retried.Applied);
        Assert.Equal(failed, retried.Explanation.Id);
        Assert.Equal(2, retried.Explanation.AttemptCount);
    }

    /// <summary>Case 4 with the current writer answering right now: no button.</summary>
    [Fact]
    public async Task Case4WithTheCurrentWriterPendingOffersNothing()
    {
        await using var scenario = await Scenario.StartAsync();
        await scenario.InsertAsync(ExplanationProvider.Anthropic, Prompt, ExplanationStatus.Pending, minutesAgo: 0);

        var shown = await scenario.ShownAsync();
        Assert.Equal(ExplanationWireNames.Pending, shown.Status);

        var asked = await ExplanationTestCorpus.RequestAsync(scenario.Client, scenario.AlertId, regenerate: true);
        Assert.Equal(HttpStatusCode.Conflict, asked.StatusCode);
        Assert.Equal("EXPLANATION_PENDING", await ExplanationTestCorpus.ProblemCodeAsync(asked));
    }

    /// <summary>
    /// Case 5: nothing ready, no row of the current writer, and another writer's failed attempt. That
    /// attempt is shown — it is what exists — and the button creates the row of the current writer.
    /// </summary>
    [Fact]
    public async Task Case5AnotherWritersFailureWithNoRowOfTheCurrentWriterOffersToWriteIt()
    {
        await using var scenario = await Scenario.StartAsync();
        var other = await scenario.InsertAsync(ExplanationProvider.Mock, Template, ExplanationStatus.Failed);

        var shown = await scenario.ShownAsync();

        Assert.Equal(other, shown.Id);
        Assert.True(shown.WrittenByAnotherTemplate);
        Assert.Null(shown.CurrentWriterAttempt);

        var written = await ExplanationTestCorpus.RequestOkAsync(scenario.Client, scenario.AlertId);

        Assert.True(written.Applied);
        Assert.NotEqual(other, written.Explanation.Id);
        Assert.Equal(1, written.Explanation.AttemptCount);
    }

    /// <summary>
    /// Case 5 with another writer of the <em>same provider</em> still pending: no button. The partial
    /// index over pending rows is unique per evaluation, provider and language, so the current
    /// writer's reservation would collide.
    /// </summary>
    [Fact]
    public async Task Case5WithAPendingRowOfTheSameProviderOffersNothing()
    {
        await using var scenario = await Scenario.StartAsync();
        var other = await scenario.InsertAsync(ExplanationProvider.Anthropic, "anthropic-p0", ExplanationStatus.Pending, minutesAgo: 0);

        var shown = await scenario.ShownAsync();

        Assert.Equal(other, shown.Id);
        Assert.Equal(ExplanationWireNames.Anthropic, shown.Provider);
        Assert.Equal(shown.Provider, shown.CurrentWriterProvider);

        var asked = await ExplanationTestCorpus.RequestAsync(scenario.Client, scenario.AlertId);
        Assert.Equal(HttpStatusCode.Conflict, asked.StatusCode);
        Assert.Equal("EXPLANATION_PENDING", await ExplanationTestCorpus.ProblemCodeAsync(asked));
    }

    /// <summary>Case 5 with a pending row of <em>another</em> provider: the button creates the row.</summary>
    [Fact]
    public async Task Case5WithAPendingRowOfAnotherProviderOffersToWrite()
    {
        await using var scenario = await Scenario.StartAsync();
        await scenario.InsertAsync(ExplanationProvider.Mock, Template, ExplanationStatus.Pending, minutesAgo: 0);

        var written = await ExplanationTestCorpus.RequestOkAsync(scenario.Client, scenario.AlertId);

        Assert.True(written.Applied);
        Assert.Equal(ExplanationWireNames.Anthropic, written.Explanation.Provider);
        Assert.Equal(1, written.Explanation.AttemptCount);
    }

    /// <summary>
    /// The review records the explanation that was shown — what the analyst had in front of her,
    /// which is decision 56 — and refuses to cite the attempt beside it, which has no text.
    /// </summary>
    [Fact]
    public async Task TheReviewCitesTheShownTextAndNeverTheAttempt()
    {
        await using var scenario = await Scenario.StartAsync();
        var paragraph = await scenario.InsertAsync(ExplanationProvider.Mock, Template, ExplanationStatus.Ready, minutesAgo: 10);
        var attempt = await scenario.InsertAsync(ExplanationProvider.Anthropic, Prompt, ExplanationStatus.Failed, minutesAgo: 2);

        var citingTheAttempt = await AlertTestCorpus.ReviewAsync(
            scenario.Client,
            scenario.AlertId,
            "CONFIRMED_SAFE",
            explanationId: attempt);
        var citingTheText = await AlertTestCorpus.ReviewAsync(
            scenario.Client,
            scenario.AlertId,
            "CONFIRMED_SAFE",
            explanationId: paragraph);

        Assert.Equal(HttpStatusCode.Conflict, citingTheAttempt.StatusCode);
        Assert.Equal("ALERT_REVIEW_EXPLANATION_UNKNOWN", await ExplanationTestCorpus.ProblemCodeAsync(citingTheAttempt));
        Assert.Equal(HttpStatusCode.OK, citingTheText.StatusCode);
        Assert.Equal(paragraph, (await AlertTestCorpus.GetAlertAsync(scenario.Client, scenario.AlertId)).Review?.ExplanationId);
    }

    /// <summary>One alert of the divergence corpus, with a model as the registered writer.</summary>
    private sealed class Scenario : IAsyncDisposable
    {
        private readonly SalvoApiFactory factory;

        private Scenario(SalvoApiFactory factory, HttpClient client, AlertListItem alert, Guid evaluationId)
        {
            this.factory = factory;
            Client = client;
            AlertId = alert.Id;
            EvaluationId = evaluationId;
        }

        public HttpClient Client { get; }

        public Guid AlertId { get; }

        public Guid EvaluationId { get; }

        public static async Task<Scenario> StartAsync()
        {
            var model = new ExplanationTestCorpus.SwitchableProvider
            {
                Provider = ExplanationProvider.Anthropic,
                TemplateVersion = Prompt,
            };
            var factory = new SalvoApiFactory
            {
                ConfigureTestServices = services => services.AddScoped<IExplanationProvider>(_ => model),
            };
            var client = await factory.CreateMigratedClientAsync();
            await AlertTestCorpus.ImportAsync(client, AlertTestCorpus.DivergenceBase());
            await AlertTestCorpus.RunScoringAsync(client);
            var alert = Assert.Single((await AlertTestCorpus.ListAlertsAsync(client)).Items);
            var detail = await AlertTestCorpus.GetAlertAsync(client, alert.Id);

            return new(factory, client, alert, detail.Snapshot.EvaluationId);
        }

        /// <summary>
        /// A row of one writer in the state the case needs, written directly. A failed row failed
        /// with a refusal the way the adapter records one; with <paramref name="attempts"/> at the
        /// budget, the domain itself turns its code into <c>ATTEMPT_LIMIT_REACHED</c>.
        /// </summary>
        public async Task<Guid> InsertAsync(
            ExplanationProvider provider,
            string version,
            ExplanationStatus status,
            int minutesAgo = 5,
            int attempts = 1)
        {
            var requestedAt = DateTimeOffset.UtcNow.AddMinutes(-minutesAgo);
            var row = AlertExplanation.Reserve(
                Guid.NewGuid(),
                EvaluationId,
                provider,
                version,
                "e4-v1",
                ExplanationLanguage.Spanish,
                AlertId,
                requestedAt);

            for (var attempt = 1; attempt < attempts; attempt++)
            {
                row.Fail(ExplanationFailureCode.ProviderRefused, null, requestedAt);
                row.Retake(AlertId, requestedAt);
            }

            switch (status)
            {
                case ExplanationStatus.Ready:
                    row.Complete("El pedido superó el umbral.", [], null, null, null, requestedAt.AddSeconds(2));
                    break;
                case ExplanationStatus.Failed:
                    row.Fail(
                        ExplanationFailureCode.ProviderRefused,
                        "cyber; req_011CSHoEeqs5C35K2UUqR7Fy",
                        requestedAt.AddSeconds(2));
                    break;
            }

            await using var scope = factory.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();
            dbContext.AlertExplanations.Add(row);
            await dbContext.SaveChangesAsync();

            return row.Id;
        }

        public async Task<AlertExplanationView> ShownAsync()
        {
            var detail = await AlertTestCorpus.GetAlertAsync(Client, AlertId);

            return Assert.IsType<AlertExplanationView>(detail.Explanation);
        }

        public async Task<int> CountAsync()
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();

            return dbContext.AlertExplanations.Count();
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await factory.DisposeAsync();
        }
    }
}
