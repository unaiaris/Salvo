namespace Salvo.Domain.Risk;

public readonly record struct RationalMultiplier
{
    public RationalMultiplier(int numerator, int denominator)
    {
        if (numerator <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(numerator), "numerator must be positive.");
        }

        if (denominator <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(denominator), "denominator must be positive.");
        }

        Numerator = numerator;
        Denominator = denominator;
    }

    public int Numerator { get; }

    public int Denominator { get; }

    public bool IsReachedBy(long value, long baseline)
    {
        return (decimal)value * Denominator >= (decimal)baseline * Numerator;
    }
}
