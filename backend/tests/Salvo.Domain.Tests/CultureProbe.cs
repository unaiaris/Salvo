using System.Globalization;

namespace Salvo.Domain.Tests;

/// <summary>
/// Runs a computation under a set of ambient cultures on dedicated threads, so that the probe never
/// leaks a culture into other tests.
/// </summary>
internal static class CultureProbe
{
    public static IReadOnlyList<T> Run<T>(IReadOnlyList<string> cultureNames, Func<T> compute)
    {
        var results = new T[cultureNames.Count];

        for (var index = 0; index < cultureNames.Count; index++)
        {
            var position = index;
            var culture = new CultureInfo(cultureNames[position]);
            var thread = new Thread(() =>
            {
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
                results[position] = compute();
            });
            thread.Start();
            thread.Join();
        }

        return results;
    }
}
