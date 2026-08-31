using System.Globalization;
using System.Text.RegularExpressions;
using Salvo.Domain.Orders;

namespace Salvo.Application.Orders.Importing;

public sealed partial class ImportOrdersHandler(
    IOrderImportParser parser,
    IOrderDataStore store,
    IOrderIdGenerator idGenerator,
    TimeProvider timeProvider)
{
    public const int MaximumRecords = 10_000;
    public const int MaximumDetailedErrors = 1_000;

    public async Task<ImportOrdersResult> HandleAsync(
        Stream stream,
        OrderImportFormat format,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var document = await parser.ParseAsync(stream, format, cancellationToken);
        if (document.Records.Count == 0)
        {
            throw new OrderImportDocumentException("EMPTY_FILE", "The import file contains no records.");
        }

        if (document.Records.Count > MaximumRecords)
        {
            throw new OrderImportDocumentException(
                "TOO_MANY_RECORDS",
                $"The import file must not exceed {MaximumRecords.ToString(CultureInfo.InvariantCulture)} records.",
                OrderImportDocumentFailure.TooManyRecords);
        }

        var createdAt = timeProvider.GetUtcNow();
        var errors = new List<ImportRecordError>();
        var invalidRecords = new HashSet<int>();
        var candidates = new List<ImportCandidate>();

        foreach (var record in document.Records)
        {
            var recordErrors = new List<ImportRecordError>(record.Errors);
            var transportErrorFields = record.Errors
                .Where(error => error.Field is not null)
                .Select(error => error.Field)
                .ToHashSet(StringComparer.Ordinal);

            var occurredAt = ParseOccurredAt(record, recordErrors);
            var amountCents = ParseAmountCents(record, recordErrors);
            transportErrorFields.UnionWith(
                recordErrors
                    .Where(error => error.Field is not null)
                    .Select(error => error.Field));

            var creation = Order.Create(new OrderDraft(
                idGenerator.Create(),
                GetValue(record, OrderImportFields.MerchantId),
                GetValue(record, OrderImportFields.MerchantReferenceId),
                GetValue(record, OrderImportFields.BuyerReferenceId),
                occurredAt,
                amountCents,
                GetValue(record, OrderImportFields.CurrencyCode),
                GetValue(record, OrderImportFields.CountryCode),
                GetValue(record, OrderImportFields.City),
                GetValue(record, OrderImportFields.Channel),
                GetValue(record, OrderImportFields.DeviceSessionId),
                createdAt));

            foreach (var domainError in creation.Errors)
            {
                if (!transportErrorFields.Contains(domainError.Field))
                {
                    recordErrors.Add(new(
                        record.RecordNumber,
                        record.LineNumber,
                        domainError.Field,
                        domainError.Code,
                        domainError.Message));
                }
            }

            if (recordErrors.Count > 0 || creation.Order is null)
            {
                errors.AddRange(recordErrors);
                invalidRecords.Add(record.RecordNumber);
                continue;
            }

            candidates.Add(new(record, creation.Order));
        }

        var uniqueCandidates = new Dictionary<OrderReference, ImportCandidate>();
        var duplicateCount = 0;

        foreach (var candidate in candidates)
        {
            if (!uniqueCandidates.TryGetValue(candidate.Order.Reference, out var first))
            {
                uniqueCandidates.Add(candidate.Order.Reference, candidate);
                continue;
            }

            if (first.Order.HasSameBusinessFactsAs(candidate.Order))
            {
                duplicateCount++;
                continue;
            }

            errors.Add(CreateReferenceConflict(candidate.Record));
            invalidRecords.Add(candidate.Record.RecordNumber);
        }

        var references = uniqueCandidates.Keys.ToArray();
        var existingOrders = await store.GetOrdersByReferencesAsync(references, cancellationToken);
        var ordersToInsert = new List<Order>();

        foreach (var candidate in uniqueCandidates.Values)
        {
            if (!existingOrders.TryGetValue(candidate.Order.Reference, out var existing))
            {
                ordersToInsert.Add(candidate.Order);
                continue;
            }

            if (existing.HasSameBusinessFactsAs(candidate.Order))
            {
                duplicateCount++;
                continue;
            }

            errors.Add(CreateReferenceConflict(candidate.Record));
            invalidRecords.Add(candidate.Record.RecordNumber);
        }

        if (ordersToInsert.Count > 0)
        {
            await store.AddOrdersAsync(ordersToInsert, cancellationToken);
        }

        var orderedErrors = errors
            .OrderBy(error => error.RecordNumber)
            .ThenBy(error => error.Field, StringComparer.Ordinal)
            .ThenBy(error => error.Code, StringComparer.Ordinal)
            .ToArray();
        var detailedErrors = orderedErrors.Take(MaximumDetailedErrors).ToArray();

        return new(
            document.Records.Count,
            ordersToInsert.Count,
            duplicateCount,
            invalidRecords.Count,
            orderedErrors.Length > detailedErrors.Length,
            detailedErrors);
    }

    private static string? GetValue(RawOrderImportRecord record, string field)
    {
        return record.Values.TryGetValue(field, out var value) ? value : null;
    }

    private static long? ParseAmountCents(
        RawOrderImportRecord record,
        List<ImportRecordError> errors)
    {
        var raw = GetValue(record, OrderImportFields.AmountCents);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (!UnsignedIntegerPattern().IsMatch(raw)
            || !long.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
        {
            errors.Add(new(
                record.RecordNumber,
                record.LineNumber,
                OrderImportFields.AmountCents,
                "INVALID_FORMAT",
                "amountCents must be a base-10 integer."));
            return null;
        }

        return value;
    }

    private static DateTimeOffset? ParseOccurredAt(
        RawOrderImportRecord record,
        List<ImportRecordError> errors)
    {
        var raw = GetValue(record, OrderImportFields.OccurredAt);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (!TimestampPattern().IsMatch(raw)
            || !DateTimeOffset.TryParse(
                raw,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var occurredAt))
        {
            errors.Add(new(
                record.RecordNumber,
                record.LineNumber,
                OrderImportFields.OccurredAt,
                "INVALID_FORMAT",
                "occurredAt must be an ISO 8601 timestamp with an explicit offset and millisecond precision or less."));
            return null;
        }

        return occurredAt.ToUniversalTime();
    }

    private static ImportRecordError CreateReferenceConflict(RawOrderImportRecord record)
    {
        return new(
            record.RecordNumber,
            record.LineNumber,
            OrderImportFields.MerchantReferenceId,
            "REFERENCE_CONFLICT",
            "The merchant reference already identifies an order with different immutable facts.");
    }

    [GeneratedRegex("^[0-9]+$", RegexOptions.CultureInvariant)]
    private static partial Regex UnsignedIntegerPattern();

    [GeneratedRegex(
        "^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(?:\\.[0-9]{1,3})?(?:Z|[+-][0-9]{2}:[0-9]{2})$",
        RegexOptions.CultureInvariant)]
    private static partial Regex TimestampPattern();

    private sealed record ImportCandidate(RawOrderImportRecord Record, Order Order);
}
