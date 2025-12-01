using System.Text.Json;

namespace Corely.IAM.DevTools.Commands.Registration;

internal partial class Registration : CommandBase
{
    public Registration()
        : base("register", "Register operations") { }

    protected override void Execute()
    {
        Console.WriteLine("Sub command missing. Use --help to see the available sub commands");
    }

    internal static void CreateSampleJson<T>(string requestJsonFile, T sample)
    {
        FileInfo file = new(requestJsonFile);

        if (!Directory.Exists(file.DirectoryName))
        {
            Console.WriteLine($"Directory not found: {file.DirectoryName}");
            return;
        }

        var registerRequests = new T[] { sample };
        var json = JsonSerializer.Serialize(registerRequests);
        File.WriteAllText(requestJsonFile, json);

        Console.WriteLine($"Sample json file created at: {requestJsonFile}");
    }

    internal static List<T>? ReadRequestJson<T>(string requestJsonFile)
    {
        if (!File.Exists(requestJsonFile))
        {
            Console.WriteLine($"File not found: {requestJsonFile}");
            return default;
        }

        var json = File.ReadAllText(requestJsonFile);
        var registerRequest = JsonSerializer.Deserialize<List<T>>(json);

        if (registerRequest == null)
        {
            Console.WriteLine($"Invalid json: {requestJsonFile}");
        }

        return registerRequest;
    }
}
