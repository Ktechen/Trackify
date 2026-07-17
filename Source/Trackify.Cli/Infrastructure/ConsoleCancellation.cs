namespace Trackify.Cli.Infrastructure;

/// <summary>Ctrl+C handling for long-running commands: cancels a token instead of killing the process.</summary>
internal static class ConsoleCancellation
{
    public static CancellationTokenSource CreateTokenSource()
    {
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cts.Cancel();
        };
        return cts;
    }
}
