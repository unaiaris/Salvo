namespace Salvo.Domain.Risk;

public sealed record RiskSignal(string Rule, int Weight, string Detail);
