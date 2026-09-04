using Salvo.Domain.Alerts;
using Salvo.Domain.External;
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
/// <param name="CurrentRun">
/// The run <paramref name="CurrentEvaluation"/> comes from. It is what dates the current block: the
/// evaluation itself carries the instant it was first computed, which a later run reuses unchanged.
/// </param>
/// <param name="ExternalEvaluation">
/// What the external provider was asked and what it answered, or <see langword="null"/> when nobody
/// has asked. It is a second opinion beside the local one and never blended into it: the provider
/// opines and the merchant decides.
/// </param>
/// <param name="HasContradictoryCallback">
/// Whether the provider ever sent a verdict that contradicted the one it had already given. Kept
/// visible rather than discarded, because a provider disagreeing with itself is a fact about the
/// integration that an analyst reading a verdict has a right to know.
/// </param>
public sealed record AlertContext(
    Alert Alert,
    Order Order,
    RiskEvaluation? CurrentEvaluation,
    AlertReview? Review,
    ScoringRunReference? CurrentRun,
    ExternalEvaluation? ExternalEvaluation = null,
    bool HasContradictoryCallback = false);
