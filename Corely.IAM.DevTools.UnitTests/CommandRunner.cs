using System.CommandLine;

namespace Corely.IAM.DevTools.UnitTests;

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
