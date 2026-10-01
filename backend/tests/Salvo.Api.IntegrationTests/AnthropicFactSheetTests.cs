using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Salvo.Application.Explanations;
using Salvo.Domain.Explanations;
using Salvo.Domain.Risk;
using Salvo.Infrastructure.Explanations;

namespace Salvo.Api.IntegrationTests;

/// <summary>
/// The sheet of facts a model reads, over every alert of the demo corpus and in both languages.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Two tests, because they protect different things</strong> (decision 78). The property —
/// every number on the sheet is grounded by the facts of its evaluation — is the mirror of the spy:
/// the spy asserts what does not reach the model, this asserts that what does can be verified. It
/// cannot see the three things the sheet must never carry, though: the amount in cents <em>is</em> a
/// fact, versions are struck out before tokenizing, and of an ISO instant only the seconds are
/// ungrounded — and the seconds of this corpus are always zero, which a field at zero can ground.
/// So the second test reads the rendered sheet for those three directly.
/// </para>
/// </remarks>
public sealed partial class AnthropicFactSheetTests
{
    private static readonly ExplanationLanguage[] Languages =
        [ExplanationLanguage.Spanish, ExplanationLanguage.Portuguese];

    /// <summary>Every number on the sheet is backed by a fact of the evaluation it describes.</summary>
    [Fact]
    public async Task EveryNumberOnTheSheetIsGrounded()
    {
        var inputs = await DemoInputsAsync();

        foreach (var input in inputs)
        {
            var facts = ExplanationFacts.For(
                input,
                SignalFacts.ForAll(input.Signals),
                RuleConfig.ForVersion(input.RuleConfigVersion));

            foreach (var language in Languages)
            {
                var sheet = AnthropicFactSheet.Render(input with { Language = language });

                foreach (var token in NumberTokenizer.Extract(sheet))
                {
                    Assert.True(
                        facts.IsGrounded(token),
                        $"'{token.Text}' is on the {language} sheet and no fact backs it:\n{sheet}");
                }
            }
        }
    }

    /// <summary>The three things the property cannot see, read off the rendered sheet.</summary>
    [Fact]
    public async Task TheSheetCarriesNoCentsNoIsoInstantAndNoVersion()
    {
        var inputs = await DemoInputsAsync();
        string[] versions = ["e3-v1", "e3-v2", "e4-v1", "e7-v1", "e7-v2", AnthropicFactSheet.PromptVersion];

        foreach (var input in inputs)
        {
            foreach (var language in Languages)
            {
                var sheet = AnthropicFactSheet.Render(input with { Language = language });

                Assert.DoesNotContain(
                    NumberTokenizer.Extract(sheet),
                    token => token.Readings.Any(reading => reading.Decimals == 0 && reading.Value == input.AmountCents));
                Assert.DoesNotMatch(IsoInstantPattern(), sheet);
                foreach (var version in versions.Append(input.RuleConfigVersion).Append(input.AlertPolicyVersion))
                {
                    Assert.DoesNotContain(version, sheet, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
    }

    /// <summary>
    /// The sheet is in the language of the deployment, as the template is: the words change and the
    /// figures do not.
    /// </summary>
    [Fact]
    public void TheSheetAndThePromptExistInBothLanguagesWithTheSameFigures()
    {
        var input = AnthropicExplanationProviderTests.Input();

        var spanish = AnthropicFactSheet.Render(input);
        var portuguese = AnthropicFactSheet.Render(input with { Language = ExplanationLanguage.Portuguese });

        Assert.Contains("Hechos de la evaluación", spanish, StringComparison.Ordinal);
        Assert.Contains("Fatos da avaliação", portuguese, StringComparison.Ordinal);
        Assert.Contains("castellano", AnthropicFactSheet.Prompt(ExplanationLanguage.Spanish), StringComparison.Ordinal);
        Assert.Contains("português", AnthropicFactSheet.Prompt(ExplanationLanguage.Portuguese), StringComparison.Ordinal);
        Assert.Equal(
            NumberTokenizer.Extract(spanish).Select(token => token.Text),
            NumberTokenizer.Extract(portuguese).Select(token => token.Text));
    }

    /// <summary>
    /// The inputs the use case hands a provider for every alert of the demo corpus, recorded rather
    /// than rebuilt, so the sheet is tested over exactly what a model would be given.
    /// </summary>
    private static async Task<IReadOnlyList<ExplanationInput>> DemoInputsAsync()
    {
        var recorder = new RecordingProvider();
        await using var factory = new SalvoApiFactory
        {
            ConfigureTestServices = services => services.AddScoped<IExplanationProvider>(_ => recorder),
        };
        using var client = await factory.CreateMigratedClientAsync();
        (await client.PostAsync("/api/demo-data/seed", null)).EnsureSuccessStatusCode();
        await AlertTestCorpus.RunScoringAsync(client);

        foreach (var alert in (await AlertTestCorpus.ListAlertsAsync(client, "?pageSize=200")).Items)
        {
            await ExplanationTestCorpus.RequestOkAsync(client, alert.Id);
        }

        Assert.NotEmpty(recorder.Inputs);

        return recorder.Inputs;
    }

    [GeneratedRegex(@"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}", RegexOptions.CultureInvariant)]
    private static partial Regex IsoInstantPattern();

    private sealed class RecordingProvider : IExplanationProvider
    {
        private readonly List<ExplanationInput> inputs = [];

        public ExplanationProvider Provider => ExplanationProvider.Anthropic;

        public string TemplateVersion => AnthropicFactSheet.PromptVersion;

        public IReadOnlyList<ExplanationInput> Inputs => inputs;

        public Task<ExplanationProviderResult> ExplainAsync(ExplanationInput input, CancellationToken cancellationToken)
        {
            inputs.Add(input);

            return Task.FromResult(ExplanationProviderResult.Refused());
        }
    }
}
