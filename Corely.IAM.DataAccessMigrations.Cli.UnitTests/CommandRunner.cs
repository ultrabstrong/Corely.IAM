using System.CommandLine;

namespace Corely.IAM.DataAccessMigrations.Cli.UnitTests;

/// <summary>
/// Runs a command through the real parse-and-invoke pipeline and captures what it wrote.
/// </summary>
/// <remarks>
/// These commands are only ever exercised through the console, so a test that called their
/// methods directly would miss the failures that actually happen: argument binding and how a
/// result is rendered for output. Both are covered by driving Parse().Invoke() and reading stdout.
/// </remarks>
internal static class CommandRunner
{
    public static string Run(Command command, params string[] args)
    {
        var original = Console.Out;
        using var captured = new StringWriter();
        Console.SetOut(captured);
        try
        {
            command.Parse(args).Invoke();
        }
        finally
        {
            Console.SetOut(original);
        }
        return captured.ToString().Trim();
    }
}
