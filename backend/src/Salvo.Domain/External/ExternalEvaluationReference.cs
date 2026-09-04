using Salvo.Domain.Orders;

namespace Salvo.Domain.External;

/// <summary>
/// The stable reference of an order, as the text an external provider is given and echoes back.
/// </summary>
/// <remarks>
/// It is the pair the order is identified by commercially, because a merchant reference is only
/// unique inside its merchant. The pair is what gets written during the reservation, before the
/// provider is called, so a result that arrives without an external identifier can still be
/// correlated.
/// </remarks>
public static class ExternalEvaluationReference
{
    public const char Separator = '|';

    public const int MaximumLength = (Order.MaximumIdentifierLength * 2) + 1;

    public static string From(OrderReference reference)
    {
        return $"{reference.MerchantId}{Separator}{reference.MerchantReferenceId}";
    }
}
