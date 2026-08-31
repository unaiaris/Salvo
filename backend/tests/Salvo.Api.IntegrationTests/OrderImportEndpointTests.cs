using System.Net;
using System.Net.Http.Json;
using System.Text;
using Salvo.Application.Orders.Importing;

namespace Salvo.Api.IntegrationTests;

public sealed class OrderImportEndpointTests : IClassFixture<SalvoApiFactory>
{
    private readonly SalvoApiFactory factory;

    public OrderImportEndpointTests(SalvoApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task CsvImportSupportsBomQuotingEscapedQuotesMultilineAndPartialErrors()
    {
        using var client = await factory.CreateMigratedClientAsync();
        var csv = "\uFEFF" + """"
            merchantId,merchantReferenceId,buyerReferenceId,occurredAt,amountCents,currencyCode,countryCode,city,channel,deviceSessionId
            MER_CSV,ORD_CSV_1,BUY_CSV_1,2026-08-01T10:00:00-03:00,12500,UYU,UY,"Montevideo, Centro",WEB,DEV_CSV_1
            MER_CSV,ORD_CSV_2,BUY_CSV_2,2026-08-01T14:00:00Z,0,UYU,UY,Salto,WEB,
            MER_CSV,ORD_CSV_3,BUY_CSV_3,2026-08-01T15:00:00Z,25000,UYU,UY,"Maldonado
            Centro",WEB,
            MER_CSV,ORD_CSV_4,BUY_CSV_4,2026-08-01T16:00:00Z,35000,UYU,UY,"Colonia ""Histórica""",MARKETPLACE,
            """";

        using var content = CreateImportContent(csv, "CSV", "orders.csv", "text/csv");
        var response = await client.PostAsync("/api/order-imports", content);
        var result = await response.Content.ReadFromJsonAsync<ImportOrdersResult>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(4, result.TotalRecords);
        Assert.Equal(2, result.ImportedCount);
        Assert.Equal(0, result.DuplicateCount);
        Assert.Equal(2, result.InvalidRecordCount);
        Assert.Contains(result.Errors, error =>
            error.RecordNumber == 2 && error.Field == "amountCents" && error.Code == "OUT_OF_RANGE");
        Assert.Contains(result.Errors, error =>
            error.RecordNumber == 3 && error.Field == "city" && error.Code == "INVALID_FORMAT");
    }

    [Fact]
    public async Task CsvImportDetectsSemicolonDelimiter()
    {
        using var client = await factory.CreateMigratedClientAsync();
        const string csv = """
            merchantId;merchantReferenceId;buyerReferenceId;occurredAt;amountCents;currencyCode;countryCode
            MER_SEMICOLON;ORD_SEMICOLON_1;BUY_SEMICOLON_1;2026-08-02T12:00:00Z;1000;BRL;BR
            """;

        using var content = CreateImportContent(csv, "csv", "orders.csv", "text/csv");
        var response = await client.PostAsync("/api/order-imports", content);
        var result = await response.Content.ReadFromJsonAsync<ImportOrdersResult>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(1, result.ImportedCount);
    }

    [Fact]
    public async Task JsonImportReturnsPartialErrorsWithoutEchoingValuesOrLabels()
    {
        using var client = await factory.CreateMigratedClientAsync();
        const string json = """
            [
              {
                "merchantId": "MER_JSON",
                "merchantReferenceId": "ORD_JSON_1",
                "buyerReferenceId": "BUY_JSON_1",
                "occurredAt": "2026-08-03T12:00:00Z",
                "amountCents": 9000,
                "currencyCode": "USD",
                "countryCode": "US"
              },
              {
                "merchantId": "MER_JSON",
                "merchantReferenceId": "ORD_SECRET_VALUE",
                "buyerReferenceId": "BUY_JSON_2",
                "occurredAt": "2026-08-03T13:00:00Z",
                "amountCents": 9100,
                "currencyCode": "USD",
                "countryCode": "US",
                "isFraudLabel": true
              }
            ]
            """;

        using var content = CreateImportContent(json, "JSON", "orders.json", "application/json");
        var response = await client.PostAsync("/api/order-imports", content);
        var responseText = await response.Content.ReadAsStringAsync();
        var result = await response.Content.ReadFromJsonAsync<ImportOrdersResult>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(1, result.InvalidRecordCount);
        Assert.Contains(result.Errors, error =>
            error.RecordNumber == 2 && error.Field is null && error.Code == "UNKNOWN_FIELD");
        Assert.DoesNotContain("ORD_SECRET_VALUE", responseText, StringComparison.Ordinal);
        Assert.DoesNotContain("isFraudLabel", responseText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExactDuplicateIsSkippedAndChangedFactsConflict()
    {
        using var client = await factory.CreateMigratedClientAsync();
        var original = CreateJsonOrder("ORD_IDEMPOTENT", 10_000);

        using (var firstContent = CreateImportContent(original, "JSON", "orders.json", "application/json"))
        {
            var first = await client.PostAsync("/api/order-imports", firstContent);
            var firstResult = await first.Content.ReadFromJsonAsync<ImportOrdersResult>();
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            Assert.Equal(1, firstResult?.ImportedCount);
        }

        using (var duplicateContent = CreateImportContent(original, "JSON", "orders.json", "application/json"))
        {
            var duplicate = await client.PostAsync("/api/order-imports", duplicateContent);
            var duplicateResult = await duplicate.Content.ReadFromJsonAsync<ImportOrdersResult>();
            Assert.Equal(HttpStatusCode.OK, duplicate.StatusCode);
            Assert.Equal(0, duplicateResult?.ImportedCount);
            Assert.Equal(1, duplicateResult?.DuplicateCount);
        }

        using var conflictContent = CreateImportContent(
            CreateJsonOrder("ORD_IDEMPOTENT", 10_001),
            "JSON",
            "orders.json",
            "application/json");
        var conflict = await client.PostAsync("/api/order-imports", conflictContent);
        var conflictResult = await conflict.Content.ReadFromJsonAsync<ImportOrdersResult>();

        Assert.Equal(HttpStatusCode.OK, conflict.StatusCode);
        Assert.Equal(0, conflictResult?.ImportedCount);
        Assert.Equal(1, conflictResult?.InvalidRecordCount);
        Assert.Contains(conflictResult?.Errors ?? [], error => error.Code == "REFERENCE_CONFLICT");
    }

    [Fact]
    public async Task StructurallyInvalidJsonReturnsStableProblemCode()
    {
        using var client = await factory.CreateMigratedClientAsync();
        using var content = CreateImportContent("[{", "JSON", "orders.json", "application/json");

        var response = await client.PostAsync("/api/order-imports", content);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("INVALID_JSON", body, StringComparison.Ordinal);
        Assert.DoesNotContain("BytePositionInLine", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FileLargerThanFiveMebibytesIsRejected()
    {
        using var client = await factory.CreateMigratedClientAsync();
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(new byte[(5 * 1024 * 1024) + 1]), "file", "orders.csv");
        content.Add(new StringContent("CSV", Encoding.UTF8), "format");

        var response = await client.PostAsync("/api/order-imports", content);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Contains("FILE_TOO_LARGE", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ErrorDetailsAreCappedWhileInvalidRecordCountRemainsExact()
    {
        using var client = await factory.CreateMigratedClientAsync();
        var json = $"[{string.Join(',', Enumerable.Repeat("{}", 1001))}]";
        using var content = CreateImportContent(json, "JSON", "orders.json", "application/json");

        var response = await client.PostAsync("/api/order-imports", content);
        var result = await response.Content.ReadFromJsonAsync<ImportOrdersResult>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(1001, result.InvalidRecordCount);
        Assert.Equal(1000, result.Errors.Count);
        Assert.True(result.ErrorsTruncated);
    }

    [Fact]
    public async Task MoreThanTenThousandRecordsIsRejectedBeforeDomainProcessing()
    {
        using var client = await factory.CreateMigratedClientAsync();
        var json = $"[{string.Join(',', Enumerable.Repeat("{}", 10_001))}]";
        using var content = CreateImportContent(json, "JSON", "orders.json", "application/json");

        var response = await client.PostAsync("/api/order-imports", content);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Contains("TOO_MANY_RECORDS", body, StringComparison.Ordinal);
    }

    private static MultipartFormDataContent CreateImportContent(
        string document,
        string format,
        string fileName,
        string mediaType)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new StringContent(document, Encoding.UTF8, mediaType);
        content.Add(fileContent, "file", fileName);
        content.Add(new StringContent(format, Encoding.UTF8), "format");
        return content;
    }

    private static string CreateJsonOrder(string merchantReferenceId, long amountCents)
    {
        return $$"""
            [
              {
                "merchantId": "MER_IDEMPOTENT",
                "merchantReferenceId": "{{merchantReferenceId}}",
                "buyerReferenceId": "BUY_IDEMPOTENT",
                "occurredAt": "2026-08-04T12:00:00Z",
                "amountCents": {{amountCents}},
                "currencyCode": "UYU",
                "countryCode": "UY"
              }
            ]
            """;
    }
}
