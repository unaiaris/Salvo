using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Salvo.Domain.Evaluation;
using Salvo.Domain.Orders;

namespace Salvo.Application.Orders.Seed;

public sealed class SeedDemoOrdersHandler(IDemoOrderSource source, IOrderDataStore store)
{
    private static readonly Guid DemoNamespace = new("df09948d-8323-4d13-b2fd-7665b18a2a3e");

    public async Task<SeedDemoOrdersResult> HandleAsync(CancellationToken cancellationToken)
    {
        var document = await source.LoadAsync(cancellationToken);
        ValidateDocumentShape(document);

        if (!DateTimeOffset.TryParse(
                document.CreatedAt,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var createdAt))
        {
            throw new InvalidOperationException("The demo fixture has an invalid createdAt timestamp.");
        }

        var candidates = document.Orders.Select(record => CreateCandidate(record, createdAt)).ToArray();
        var references = candidates.Select(candidate => candidate.Order.Reference).ToArray();
        var existingOrders = await store.GetOrdersByReferencesAsync(references, cancellationToken);
        var ordersToInsert = new List<Order>();
        var effectiveOrders = new List<EffectiveSeedOrder>(candidates.Length);
        var duplicateOrders = 0;

        foreach (var candidate in candidates)
        {
            if (!existingOrders.TryGetValue(candidate.Order.Reference, out var existing))
            {
                ordersToInsert.Add(candidate.Order);
                effectiveOrders.Add(new(candidate.Order, candidate.IsFraudLabel));
                continue;
            }

            if (!existing.HasSameBusinessFactsAs(candidate.Order))
            {
                throw new DemoSeedConflictException(
                    "The demo dataset conflicts with an existing merchant order reference.");
            }

            duplicateOrders++;
            effectiveOrders.Add(new(existing, candidate.IsFraudLabel));
        }

        var orderIds = effectiveOrders.Select(item => item.Order.Id).ToArray();
        var existingLabels = await store.GetLabelsByOrderIdsAsync(orderIds, cancellationToken);
        var labelsToInsert = new List<OrderEvaluationLabel>();

        foreach (var item in effectiveOrders)
        {
            if (!existingLabels.TryGetValue(item.Order.Id, out var existingLabel))
            {
                labelsToInsert.Add(new(item.Order.Id, item.IsFraudLabel, createdAt));
                continue;
            }

            if (existingLabel.IsFraudLabel != item.IsFraudLabel)
            {
                throw new DemoSeedConflictException(
                    "The demo dataset conflicts with an existing evaluation label.");
            }
        }

        if (ordersToInsert.Count > 0 || labelsToInsert.Count > 0)
        {
            await store.AddSeedDataAsync(ordersToInsert, labelsToInsert, cancellationToken);
        }

        return new(
            document.Version,
            document.Orders.Count,
            ordersToInsert.Count,
            duplicateOrders,
            document.Orders.Count,
            labelsToInsert.Count,
            document.Orders.Count(order => order.IsFraudLabel));
    }

    private static void ValidateDocumentShape(DemoOrderDocument document)
    {
        if (!string.Equals(document.Version, "1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The demo fixture version is not supported.");
        }

        if (document.Orders.Count != 300)
        {
            throw new InvalidOperationException("The demo fixture must contain exactly 300 orders.");
        }

        if (document.Orders.Count(order => order.IsFraudLabel) != 18)
        {
            throw new InvalidOperationException("The demo fixture must contain exactly 18 fraud labels.");
        }
    }

    private static SeedCandidate CreateCandidate(DemoOrderRecord record, DateTimeOffset createdAt)
    {
        if (!DateTimeOffset.TryParse(
                record.OccurredAt,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var occurredAt))
        {
            throw new InvalidOperationException("The demo fixture contains an invalid occurredAt timestamp.");
        }

        var referenceValue = string.Concat(
            record.MerchantId.Trim().ToUpperInvariant(),
            ":",
            record.MerchantReferenceId.Trim().ToUpperInvariant());
        var id = CreateDeterministicGuid(DemoNamespace, referenceValue);
        var creation = Order.Create(new(
            id,
            record.MerchantId,
            record.MerchantReferenceId,
            record.BuyerReferenceId,
            occurredAt,
            record.AmountCents,
            record.CurrencyCode,
            record.CountryCode,
            record.City,
            record.Channel,
            record.DeviceSessionId,
            createdAt));

        if (!creation.IsSuccess || creation.Order is null)
        {
            var codes = string.Join(",", creation.Errors.Select(error => error.Code));
            throw new InvalidOperationException($"The demo fixture violates domain invariants: {codes}.");
        }

        return new(creation.Order, record.IsFraudLabel);
    }

    private static Guid CreateDeterministicGuid(Guid namespaceId, string value)
    {
        var namespaceBytes = namespaceId.ToByteArray();
        var valueBytes = Encoding.UTF8.GetBytes(value);
        var input = new byte[namespaceBytes.Length + valueBytes.Length];
        Buffer.BlockCopy(namespaceBytes, 0, input, 0, namespaceBytes.Length);
        Buffer.BlockCopy(valueBytes, 0, input, namespaceBytes.Length, valueBytes.Length);

        var hash = SHA256.HashData(input);
        var guidBytes = hash[..16];
        guidBytes[7] = (byte)((guidBytes[7] & 0x0F) | 0x80);
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);
        return new Guid(guidBytes);
    }

    private sealed record SeedCandidate(Order Order, bool IsFraudLabel);

    private sealed record EffectiveSeedOrder(Order Order, bool IsFraudLabel);
}
