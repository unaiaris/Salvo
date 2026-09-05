namespace Salvo.Application.Providers;

/// <summary>
/// What became of one call to something outside this process.
/// </summary>
internal enum ProviderCallStatus
{
    /// <summary>The provider answered.</summary>
    Completed = 1,

    /// <summary>The explicit timeout of the port elapsed first.</summary>
    TimedOut = 2,

    /// <summary>The caller went away. Whether that closes anything is the caller's decision.</summary>
    CallerCancelled = 3,

    /// <summary>The provider threw. What it threw does not cross this boundary.</summary>
    Faulted = 4,
}

internal sealed record ProviderCallResult<T>(ProviderCallStatus Status, T? Value);

/// <summary>
/// The one place a provider of any kind is actually invoked, and the one place the shape of its
/// failure is decided.
/// </summary>
/// <remarks>
/// <para>
/// Shared by the antifraud exchange and the explanation exchange on purpose. The two map the
/// outcomes differently — an antifraud timeout leaves an evaluation pending while an explanation
/// timeout fails it — but they must not disagree about <em>what a timeout is</em>. Two copies of
/// this classification would drift, and the drift would only show up the day a real provider
/// misbehaves.
/// </para>
/// <para>
/// Every call carries an explicit timeout, as the project rules require, and no exception type of
/// any client library escapes.
/// </para>
/// </remarks>
internal static class ProviderCall
{
    public static async Task<ProviderCallResult<T>> InvokeAsync<T>(
        Func<CancellationToken, Task<T>> call,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);

        try
        {
            return new(ProviderCallStatus.Completed, await call(timeoutSource.Token));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new(ProviderCallStatus.CallerCancelled, default);
        }
        catch (OperationCanceledException)
        {
            return new(ProviderCallStatus.TimedOut, default);
        }
        catch (TimeoutException)
        {
            return new(ProviderCallStatus.TimedOut, default);
        }
#pragma warning disable CA1031 // A provider that fails in an unforeseen way must not take the request down with it.
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return new(ProviderCallStatus.Faulted, default);
        }
#pragma warning restore CA1031
    }
}
