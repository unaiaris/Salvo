using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Salvo.Application.Orders.Importing;

namespace Salvo.Infrastructure.Importing;

public static class CsvOrderImportParser
{
    public static async Task<OrderImportDocument> ParseAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            DetectColumnCountChanges = true,
            DetectDelimiter = true,
            DetectDelimiterValues = [",", ";"],
            HasHeaderRecord = true,
            HeaderValidated = null,
            MissingFieldFound = null,
            TrimOptions = TrimOptions.None,
        };

        try
        {
            using var reader = new StreamReader(
                stream,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true),
                detectEncodingFromByteOrderMarks: true,
                leaveOpen: true);
            using var csv = new CsvReader(reader, configuration);

            if (!await csv.ReadAsync())
            {
                return new(Array.Empty<RawOrderImportRecord>());
            }

            csv.ReadHeader();
            var headers = csv.HeaderRecord ?? Array.Empty<string>();
            ValidateHeaders(headers);

            var records = new List<RawOrderImportRecord>();
            while (await csv.ReadAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var recordNumber = records.Count + 1;
                var values = new Dictionary<string, string?>(StringComparer.Ordinal);
                foreach (var header in headers)
                {
                    values[header] = csv.GetField(header);
                }

                records.Add(new(
                    recordNumber,
                    csv.Parser.RawRow,
                    values,
                    Array.Empty<ImportRecordError>()));

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
        catch (DecoderFallbackException exception)
        {
            throw new OrderImportDocumentException(
                "INVALID_ENCODING",
                "CSV files must use valid UTF-8 encoding.",
                innerException: exception);
        }
        catch (CsvHelperException exception)
        {
            throw new OrderImportDocumentException(
                "INVALID_CSV",
                "The CSV document is structurally invalid.",
                innerException: exception);
        }
    }

    private static void ValidateHeaders(string[] headers)
    {
        if (headers.Length == 0)
        {
            throw new OrderImportDocumentException("MISSING_HEADER", "The CSV document requires a header row.");
        }

        if (headers.Any(string.IsNullOrWhiteSpace))
        {
            throw new OrderImportDocumentException("INVALID_HEADER", "CSV headers must not be empty.");
        }

        if (headers.Length != headers.Distinct(StringComparer.Ordinal).Count())
        {
            throw new OrderImportDocumentException("DUPLICATE_HEADER", "CSV headers must be unique.");
        }

        if (headers.Any(header => !OrderImportFields.All.Contains(header)))
        {
            throw new OrderImportDocumentException("UNKNOWN_HEADER", "The CSV document contains an unknown header.");
        }

        if (OrderImportFields.Required.Any(required => !headers.Contains(required, StringComparer.Ordinal)))
        {
            throw new OrderImportDocumentException("MISSING_HEADER", "The CSV document is missing a required header.");
        }
    }
}
