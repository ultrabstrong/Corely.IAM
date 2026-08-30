using System.Diagnostics;

namespace Corely.IAM.IntegrationTests.Providers;

/// <summary>
/// Docker is startable on request rather than always running, so the provider matrix reports
/// itself skipped instead of failing when the daemon is down. That keeps the default
/// edit-build-test loop fast and dependency-free while leaving the tier one command away.
/// </summary>
public static class DockerAvailability
{
    private static readonly Lazy<string?> _unavailableReason = new(Probe);

    /// <summary>Null when Docker is usable; otherwise the reason to show as a skip message.</summary>
    public static string? UnavailableReason => _unavailableReason.Value;

    private static string? Probe()
    {
        // Opt-in rather than opt-out. Spinning three database containers takes minutes, which is
        // fine for a deliberate provider-matrix run and unacceptable in the edit-build-test loop.
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

/// <summary>
/// A Fact that skips itself when Docker is unavailable. xUnit v2 has no dynamic skip, so the
/// decision is made at discovery time by a subclassed attribute.
/// </summary>
public sealed class RequiresDockerFactAttribute : FactAttribute
{
    public RequiresDockerFactAttribute()
    {
        if (DockerAvailability.UnavailableReason is { } reason)
            Skip = reason;
    }
}

/// <summary>Theory counterpart of <see cref="RequiresDockerFactAttribute"/>.</summary>
public sealed class RequiresDockerTheoryAttribute : TheoryAttribute
{
    public RequiresDockerTheoryAttribute()
    {
        if (DockerAvailability.UnavailableReason is { } reason)
            Skip = reason;
    }
}
