using System.Text.Json;

namespace Corely.IAM.DevTools.Commands;

internal static class SampleJsonFileHelper
{
    internal static void CreateSampleMultipleRequestJson<T>(string requestJsonFile, T sample)
    {
        FileInfo file = new(requestJsonFile);

        if (!Directory.Exists(file.DirectoryName))
        {
            Console.WriteLine($"Directory not found: {file.DirectoryName}");
            return;
        }

        var requests = new T[] { sample };
        var json = JsonSerializer.Serialize(requests);
        File.WriteAllText(requestJsonFile, json);

        Console.WriteLine($"Sample json file created at: {requestJsonFile}");
    }

    internal static void CreateSampleSingleRequestJson<T>(string requestJsonFile, T sample)
    {
        FileInfo file = new(requestJsonFile);

        if (!Directory.Exists(file.DirectoryName))
        {
            Console.WriteLine($"Directory not found: {file.DirectoryName}");
            return;
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(sample, options);
        File.WriteAllText(requestJsonFile, json);

        Console.WriteLine($"Sample json file created at: {requestJsonFile}");
    }

    internal static List<T>? ReadMultipleRequestJson<T>(string requestJsonFile)
    {
        if (!File.Exists(requestJsonFile))
        {
            Console.WriteLine($"File not found: {requestJsonFile}");
            return default;
        }

        var json = File.ReadAllText(requestJsonFile);
        var request = JsonSerializer.Deserialize<List<T>>(json);

        if (request == null)
        {
            Console.WriteLine($"Invalid json: {requestJsonFile}");
        }

        return request;
    }

    internal static T? ReadSingleRequestJson<T>(string requestJsonFile)
    {
        if (!File.Exists(requestJsonFile))
        {
            Console.WriteLine($"File not found: {requestJsonFile}");
            return default;
        }

        var json = File.ReadAllText(requestJsonFile);
        var request = JsonSerializer.Deserialize<T>(json);

        if (request == null)
        {
            Console.WriteLine($"Invalid json: {requestJsonFile}");
        }

        return request;
    }
}
