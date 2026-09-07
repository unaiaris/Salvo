using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Salvo.Domain.Evaluation;
using Salvo.Domain.Orders;

namespace Salvo.Application.Orders.Seed;

public sealed class SeedDemoOrdersHandler(IDemoOrderSource source, IOrderDataStore store)
{
    private static readonly Guid DemoNamespace = new("df09948d-8323-4d13-b2fd-7665b18a2a3e");

    /// <summary>
    /// Loads the demo corpus, or refuses without writing anything.
    /// </summary>
    /// <exception cref="DemoSeedConflictException">
    /// The database already holds orders with these merchant references and different facts.
    /// </exception>
    public async Task<SeedDemoOrdersResult> HandleAsync(CancellationToken cancellationToken)
    {
        var plan = await PlanAsync(cancellationToken);

        if (plan.Conflict is { } reason)
        {
            throw new DemoSeedConflictException(reason, Describe(reason));
        }

        if (plan.OrdersToInsert.Count > 0 || plan.LabelsToInsert.Count > 0)
        {
            await store.AddSeedDataAsync(plan.OrdersToInsert, plan.LabelsToInsert, cancellationToken);
        }

        return new(
            plan.Shape.Version,
            plan.Shape.OrderCount,
            plan.OrdersToInsert.Count,
            plan.DuplicateOrders,
            plan.Shape.OrderCount,
            plan.LabelsToInsert.Count,
            plan.Shape.FraudLabelCount);
    }

    /// <summary>
    /// The same comparison, reported instead of applied. Nothing is written on this path.
    /// </summary>
    public async Task<DemoSeedPreviewResult> PreviewAsync(CancellationToken cancellationToken)
    {
        var plan = await PlanAsync(cancellationToken);

        return new(
            plan.Shape.Version,
            plan.Shape.OrderCount,
            plan.OrdersToInsert.Count,
            plan.DuplicateOrders,
            plan.LabelsToInsert.Count,
            plan.Conflict is { } reason ? DemoSeedWireNames.ToWire(reason) : null);
    }

    /// <summary>
    /// Reads the fixture, compares it against what is stored, and decides what would happen.
    /// </summary>
    /// <remarks>
    /// The conflict is discovered here, before any write, which is what makes refusing free: the
    /// corpus is never half loaded. Classifying it is not a heuristic either — only this handler
    /// writes ground-truth labels, so a colliding order that has one came from an earlier version
    /// of this corpus.
    /// </remarks>
    private async Task<SeedPlan> PlanAsync(CancellationToken cancellationToken)
    {
        var shape = DemoDatasetShape.Current;
        var document = await source.LoadAsync(cancellationToken);
        ValidateDocumentShape(document, shape);

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

        // Labels of the orders that are already there, keyed by their stored identifier: a seeded
        // order keeps the deterministic identifier of its reference, an imported one does not, and
        // either way the label is what distinguishes them.
        var existingLabels = await store.GetLabelsByOrderIdsAsync(
            existingOrders.Values.Select(order => order.Id).ToArray(),
            cancellationToken);

        var ordersToInsert = new List<Order>();
        var effectiveOrders = new List<EffectiveSeedOrder>(candidates.Length);
        var duplicateOrders = 0;
        DemoSeedConflictReason? conflict = null;

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
                conflict = Escalate(conflict, existingLabels.ContainsKey(existing.Id));
                continue;
            }

            duplicateOrders++;
            effectiveOrders.Add(new(existing, candidate.IsFraudLabel));
        }

        var labelsToInsert = new List<OrderEvaluationLabel>();
        var storedLabels = await store.GetLabelsByOrderIdsAsync(
            effectiveOrders.Select(item => item.Order.Id).ToArray(),
            cancellationToken);

        foreach (var item in effectiveOrders)
        {
            if (!storedLabels.TryGetValue(item.Order.Id, out var storedLabel))
            {
                labelsToInsert.Add(new(item.Order.Id, item.IsFraudLabel, createdAt));
                continue;
            }

            if (storedLabel.IsFraudLabel != item.IsFraudLabel)
            {
                // The facts match and the ground truth does not, which only an earlier version of
                // this corpus can produce.
                conflict = DemoSeedConflictReason.PreviousCorpus;
            }
        }

        return new(shape, ordersToInsert, labelsToInsert, duplicateOrders, conflict);
    }

    /// <summary>
    /// One labelled collision is enough to name the cause, and it wins over an unlabelled one: a
    /// database can hold the previous corpus <em>and</em> a manual import at the same time, and the
    /// sentence that helps is the one about the corpus.
    /// </summary>
    private static DemoSeedConflictReason Escalate(DemoSeedConflictReason? current, bool labelled)
    {
        if (labelled || current == DemoSeedConflictReason.PreviousCorpus)
        {
            return DemoSeedConflictReason.PreviousCorpus;
        }

        return DemoSeedConflictReason.ImportedOrders;
    }

    private static string Describe(DemoSeedConflictReason reason)
    {
        return reason == DemoSeedConflictReason.PreviousCorpus
            ? "This database holds a previous version of the demo corpus. Loading version "
              + $"{DemoDatasetShape.Current.Version} needs a new database: an order is immutable, so "
              + "the two versions cannot coexist under the same merchant references."
            : "The demo dataset conflicts with imported orders that use the same merchant "
              + "references.";
    }

    private static void ValidateDocumentShape(DemoOrderDocument document, DemoDatasetShape shape)
    {
        if (!string.Equals(document.Version, shape.Version, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"The demo fixture declares version '{document.Version}' and this build ships "
                + $"'{shape.Version}'.");
        }

        if (document.Orders.Count != shape.OrderCount)
        {
            throw new InvalidOperationException(
                $"The demo fixture must contain exactly {shape.OrderCount.ToString(CultureInfo.InvariantCulture)} orders.");
        }

        if (document.Orders.Count(order => order.IsFraudLabel) != shape.FraudLabelCount)
        {
            throw new InvalidOperationException(
                $"The demo fixture must contain exactly {shape.FraudLabelCount.ToString(CultureInfo.InvariantCulture)} fraud labels.");
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

    private sealed record SeedPlan(
        DemoDatasetShape Shape,
        IReadOnlyList<Order> OrdersToInsert,
        IReadOnlyList<OrderEvaluationLabel> LabelsToInsert,
        int DuplicateOrders,
        DemoSeedConflictReason? Conflict);
}
