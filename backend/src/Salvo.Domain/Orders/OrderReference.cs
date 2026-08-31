namespace Salvo.Domain.Orders;

public readonly record struct OrderReference(string MerchantId, string MerchantReferenceId);
