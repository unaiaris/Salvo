using System.Globalization;
using System.Text.Json;
using Salvo.Application.Orders.Importing;

namespace Salvo.Infrastructure.Importing;

public static class JsonOrderImportParser
{
    public static async Task<OrderImportDocument> ParseAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        try
        {
            using var document = await JsonDocument.ParseAsync(
                stream,
                new JsonDocumentOptions
                {
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                    MaxDepth = 16,
                },
                cancellationToken);

            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new OrderImportDocumentException(
                    "INVALID_JSON_ROOT",
                    "The JSON document root must be an array.");
            }

            var records = new List<RawOrderImportRecord>();
            foreach (var element in document.RootElement.EnumerateArray())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var recordNumber = records.Count + 1;
                records.Add(ParseRecord(element, recordNumber));

                if (records.Count > ImportOrdersHandler.MaximumRecords)
                {
                    throw new OrderImportDocumentException(
                        "TOO_MANY_RECORDS",
                        $"The import file must not exceed {ImportOrdersHandler.MaximumRecords.ToString(CultureInfo.InvariantCulture)} records.",
                        OrderImportDocumentFailure.TooManyRecords);
                }
            }

            return new(records);
        }
        catch (OrderImportDocumentException)
        {
            throw;
        }
        catch (JsonException exception)
        {
            throw new OrderImportDocumentException(
                "INVALID_JSON",
                "The JSON document is structurally invalid.",
                innerException: exception);
        }
    }

    private static RawOrderImportRecord ParseRecord(JsonElement element, int recordNumber)
    {
        var values = new Dictionary<string, string?>(StringComparer.Ordinal);
        var errors = new List<ImportRecordError>();

        if (element.ValueKind != JsonValueKind.Object)
        {
            errors.Add(new(
                recordNumber,
                null,
                null,
                "INVALID_FORMAT",
                "Each JSON array item must be an object."));
            return new(recordNumber, null, values, errors);
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
        {
            if (!OrderImportFields.All.Contains(property.Name))
            {
                errors.Add(new(
                    recordNumber,
                    null,
                    null,
                    "UNKNOWN_FIELD",
                    "The record contains an unknown field."));
                continue;
            }

            if (!seen.Add(property.Name))
            {
                errors.Add(new(
                    recordNumber,
                    null,
                    property.Name,
                    "INVALID_FORMAT",
                    "The record contains a duplicate field."));
                continue;
            }

            if (property.Value.ValueKind == JsonValueKind.Null)
            {
                values[property.Name] = null;
                continue;
            }

            if (property.Name == OrderImportFields.AmountCents)
            {
                if (property.Value.ValueKind == JsonValueKind.Number)
                {
                    values[property.Name] = property.Value.GetRawText();
                }
                else
                {
                    AddInvalidType(errors, recordNumber, property.Name, "a JSON integer");
                }

                continue;
            }

            if (property.Value.ValueKind == JsonValueKind.String)
            {
                values[property.Name] = property.Value.GetString();
            }
            else
            {
                AddInvalidType(errors, recordNumber, property.Name, "a JSON string");
            }
        }

        return new(recordNumber, null, values, errors);
    }

    private static void AddInvalidType(
        List<ImportRecordError> errors,
        int recordNumber,
        string field,
        string expectedType)
    {
        errors.Add(new(
            recordNumber,
            null,
            field,
            "INVALID_FORMAT",
            $"{field} must be {expectedType}."));
    }
}
