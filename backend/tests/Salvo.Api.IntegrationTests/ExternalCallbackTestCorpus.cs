using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Salvo.Domain.External;
using Salvo.Infrastructure.Persistence;

namespace Salvo.Api.IntegrationTests;

/// <summary>
/// Posting callbacks the way a provider would, and reading back what the API made of them.
/// </summary>
internal static class ExternalCallbackTestCorpus
{
    public const string Secret = "test-callback-secret";

    public static readonly DateTimeOffset ProviderInstant =
        new(2026, 8, 21, 9, 30, 0, TimeSpan.Zero);

    /// <summary>
    /// Posts a raw JSON body, which is how the payload-shape tests reach the endpoint: a typed
    /// record could not carry a field the contract has never heard of.
    /// </summary>
    public static async Task<HttpResponseMessage> PostRawAsync(
        HttpClient client,
        string json,
        string provider = ExternalEvaluationWireNames.ExternalMock,
        string? secret = Secret)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/external-callbacks/{provider}")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

        if (secret is not null)
        {
            request.Headers.Add(ExternalCallbackEndpoints.SecretHeader, secret);
        }

        return await client.SendAsync(request);
    }

    public static Task<HttpResponseMessage> PostAsync(
        HttpClient client,
        string? externalEvaluationId,
        string? referenceId,
        string status,
        int? score = null,
        DateTimeOffset? occurredAt = null,
        string provider = ExternalEvaluationWireNames.ExternalMock,
        string? secret = Secret)
    {
        var body = JsonSerializer.Serialize(new
        {
            externalEvaluationId,
            referenceId,
            status,
            score,
            occurredAt = occurredAt ?? ProviderInstant,
        });

        return PostRawAsync(client, body, provider, secret);
    }

    public static async Task<ExternalCallbackResponse> PostOkAsync(
        HttpClient client,
        string? externalEvaluationId,
        string? referenceId,
        string status,
        int? score = null,
        DateTimeOffset? occurredAt = null)
    {
        using var response = await PostAsync(
            client,
            externalEvaluationId,
            referenceId,
            status,
            score,
            occurredAt);

        response.EnsureSuccessStatusCode();

        return Assert.IsType<ExternalCallbackResponse>(
            await response.Content.ReadFromJsonAsync<ExternalCallbackResponse>());
    }

    public static async Task<List<CallbackReceipt>> ReadReceiptsAsync(SalvoApiFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();

        return await dbContext.CallbackReceipts
            .AsNoTracking()
            .OrderBy(receipt => receipt.ReceivedAt)
            .ThenBy(receipt => receipt.Id)
            .ToListAsync();
    }

    public static async Task<ExternalEvaluation> ReadEvaluationAsync(SalvoApiFactory factory, Guid id)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SalvoDbContext>();

        return await dbContext.ExternalEvaluations.AsNoTracking().SingleAsync(row => row.Id == id);
    }
}
