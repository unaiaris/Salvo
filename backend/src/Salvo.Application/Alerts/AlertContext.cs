using Salvo.Domain.Alerts;
using Salvo.Domain.Orders;
using Salvo.Domain.Risk;

namespace Salvo.Application.Alerts;

/// <summary>
/// An alert together with everything a reader needs to judge it: the order it belongs to, the
/// evaluation that is current for that order right now, and the verdict if one was already given.
/// </summary>
/// <param name="CurrentEvaluation">
/// The evaluation the latest scoring run referenced for the order. It is <see langword="null"/>
/// only when no run covers the order, which cannot happen once an alert exists.
/// </param>
public sealed record AlertContext(
    Alert Alert,
    Order Order,
    RiskEvaluation? CurrentEvaluation,
    AlertReview? Review);
