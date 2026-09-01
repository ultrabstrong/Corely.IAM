using System.Diagnostics;

namespace Corely.IAM.IntegrationTests.Providers;

public static class DockerAvailability
{
    private static readonly Lazy<string?> _unavailableReason = new(Probe);

    public static string? UnavailableReason => _unavailableReason.Value;

    private static string? Probe()
    {
        if (Environment.GetEnvironmentVariable("CORELY_RUN_CONTAINER_TESTS") != "1")
            return "Set CORELY_RUN_CONTAINER_TESTS=1 to run the provider matrix.";

        try
        {
            using var process = Process.Start(
                new ProcessStartInfo("docker", "info --format {{.ServerVersion}}")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            );

            if (process is null)
                return "Could not start the docker CLI.";

            if (!process.WaitForExit(TimeSpan.FromSeconds(20)))
            {
                process.Kill(entireProcessTree: true);
                return "Timed out waiting for the Docker daemon.";
            }

            return process.ExitCode == 0
                ? null
                : "Docker daemon is not running. Start Docker Desktop and re-run.";
        }
        catch (Exception ex)
        {
            return $"Docker is not available: {ex.Message}";
        }
    }
}

public sealed class RequiresDockerFactAttribute : FactAttribute
{
    public RequiresDockerFactAttribute()
    {
        if (DockerAvailability.UnavailableReason is { } reason)
            Skip = reason;
    }
}

public sealed class RequiresDockerTheoryAttribute : TheoryAttribute
{
    public RequiresDockerTheoryAttribute()
    {
        if (DockerAvailability.UnavailableReason is { } reason)
            Skip = reason;
    }
}
