using System.CommandLine;
using System.Reflection;
using System.Text.Json;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Services;
using Corely.IAM.Users.Models;

namespace Corely.IAM.DevTools.Commands;

internal abstract class CommandBase : Command
{
    private const string _helpFlag = "--help";

    private readonly Dictionary<PropertyInfo, string> _boundNames = [];

    protected CommandBase(string name, string description, string additionalDescription)
        : this(name, $"{description}{Environment.NewLine}{additionalDescription}") { }

    protected CommandBase(string name, string description)
        : base(name, description)
    {
        foreach (var property in DeclaredProperties())
        {
            var optionAttribute = property.GetCustomAttribute<OptionAttribute>();
            if (optionAttribute == null)
            {
                var argumentAttribute = property.GetCustomAttribute<ArgumentAttribute>();
                if (CreateArgument(property, argumentAttribute, out var argument))
                {
                    _boundNames[property] = argument.Name;
                    Arguments.Add(argument);
                }
            }
            else if (CreateOption(property, optionAttribute, out var option))
            {
                _boundNames[property] = option.Name;
                Options.Add(option);
            }
        }

        SetAction((parseResult, _) => InvokeExecute(parseResult));
    }

    private IEnumerable<PropertyInfo> DeclaredProperties() =>
        GetType()
            .GetProperties(
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly
            );

    private bool CreateArgument(
        PropertyInfo property,
        ArgumentAttribute? argumentAttribute,
        out Argument argument
    )
    {
        var argumentGenericType = typeof(Argument<>).MakeGenericType(property.PropertyType);
        var isRequired = argumentAttribute?.IsRequired ?? false;
        var optionalText = isRequired ? string.Empty : "[Optional] ";

        if (Activator.CreateInstance(argumentGenericType, [property.Name]) is not Argument arg)
        {
            argument = null!;
            return false;
        }

        arg.Description = $"{optionalText}{argumentAttribute?.Description}";

        if (argumentAttribute?.ArgumentArity != null)
        {
            arg.Arity = argumentAttribute.ArgumentArity.Value;
        }
        else if (!isRequired)
        {
            arg.Arity = ArgumentArity.ZeroOrOne;
        }

        if (!isRequired)
        {
            SetDefaultValue(arg, property.PropertyType, property.GetValue(this));
        }

        argument = arg;
        return true;
    }

    private bool CreateOption(
        PropertyInfo property,
        OptionAttribute optionAttribute,
        out Option option
    )
    {
        var optionGenericType = typeof(Option<>).MakeGenericType(property.PropertyType);

        var aliases = optionAttribute.Aliases;
        var name = aliases.OrderByDescending(a => a.Length).First();
        var rest = aliases.Where(a => a != name).ToArray();

        if (Activator.CreateInstance(optionGenericType, [name, rest]) is not Option opt)
        {
            option = null!;
            return false;
        }

        opt.Description = optionAttribute.Description;

        if (optionAttribute.ArgumentArity != null)
        {
            opt.Arity = optionAttribute.ArgumentArity.Value;
        }
        SetDefaultValue(opt, property.PropertyType, property.GetValue(this));

        option = opt;
        return true;
    }

    private static void SetDefaultValue(object symbol, Type valueType, object? value)
    {
        typeof(CommandBase)
            .GetMethod(nameof(SetDefaultValueCore), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(valueType)
            .Invoke(null, [symbol, value]);
    }

    private static void SetDefaultValueCore<T>(object symbol, object? value)
    {
        var typed = value is null ? default! : (T)value;
        switch (symbol)
        {
            case Option<T> option:
                option.DefaultValueFactory = _ => typed;
                break;
            case Argument<T> argument:
                argument.DefaultValueFactory = _ => typed;
                break;
        }
    }

    private async Task InvokeExecute(ParseResult parseResult)
    {
        foreach (var property in DeclaredProperties())
        {
            if (!_boundNames.TryGetValue(property, out var name))
            {
                continue;
            }

            var value = GetParsedValue(parseResult, property.PropertyType, name);
            if (value != null)
            {
                property.SetValue(this, value);
            }
        }

        try
        {
            await ExecuteAsync();
        }
        catch (Exception ex)
            when (ex is ArgumentException
                || ex is ArgumentNullException
                || ex is NotSupportedException
            )
        {
            ShowHelp(ex.Message);
        }
    }

    private static object? GetParsedValue(ParseResult parseResult, Type valueType, string name) =>
        typeof(ParseResult)
            .GetMethods()
            .Single(m =>
                m.Name == nameof(ParseResult.GetValue)
                && m.IsGenericMethodDefinition
                && m.GetParameters() is [{ ParameterType: var p }]
                && p == typeof(string)
            )
            .MakeGenericMethod(valueType)
            .Invoke(parseResult, [name]);

    protected virtual Task ExecuteAsync()
    {
        Execute();
        return Task.CompletedTask;
    }

    protected virtual void Execute() { }

    private bool _showingHelp;

    protected void ShowHelp(string message = null)
    {
        if (!string.IsNullOrEmpty(message))
        {
            Warn(message);
            Console.WriteLine();
        }

        if (_showingHelp)
        {
            return;
        }

        _showingHelp = true;
        try
        {
            Parse(_helpFlag).Invoke();
        }
        finally
        {
            _showingHelp = false;
        }
    }

    protected static void Success(string message)
    {
        WriteColored(message, ConsoleColor.Green);
    }

    protected static void Success(IEnumerable<string> messages)
    {
        WriteColored(messages, ConsoleColor.Green);
    }

    protected static void Info(string message)
    {
        Console.WriteLine(message);
    }

    protected static void Info(IEnumerable<string> messages)
    {
        Console.WriteLine(string.Join(Environment.NewLine, messages));
    }

    protected static void Warn(string message)
    {
        WriteColored(message, ConsoleColor.Yellow);
    }

    protected static void Warn(IEnumerable<string> messages)
    {
        WriteColored(messages, ConsoleColor.Yellow);
    }

    protected static void Error(string message)
    {
        WriteColored(message, ConsoleColor.Red);
    }

    protected static void Error(IEnumerable<string> messages)
    {
        WriteColored(messages, ConsoleColor.Red);
    }

    protected static void WriteColored(string message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    protected static void WriteColored(IEnumerable<string> messages, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(string.Join(Environment.NewLine, messages));
        Console.ResetColor();
    }

    protected static bool ValidateSettings()
    {
        var result = ConfigurationValidator.ValidateSettingsFile();
        if (!result.IsValid)
        {
            Error(result.ErrorMessage!);
            if (!string.IsNullOrEmpty(result.Guidance))
            {
                Info(result.Guidance);
            }
            return false;
        }
        return true;
    }

    protected static bool ValidateFullConfiguration()
    {
        var result = ConfigurationValidator.ValidateFullConfiguration();
        if (!result.IsValid)
        {
            Error(result.ErrorMessage!);
            if (!string.IsNullOrEmpty(result.Guidance))
            {
                Info(result.Guidance);
            }
            return false;
        }
        return true;
    }

    protected static void ClearAuthTokenFile()
    {
        try
        {
            var authFilePath = ConfigurationProvider.AuthTokenFilePath;
            if (File.Exists(authFilePath))
            {
                File.Delete(authFilePath);
                Info("Auth token cleared.");
            }
        }
        catch (Exception ex)
        {
            Warn($"Failed to clear auth token file: {ex.Message}");
        }
    }

    protected static bool FileExists(string filePath)
    {
        if (File.Exists(filePath))
            return true;

        Error($"File not found: {filePath}");
        Info("Use --create to create a sample request file.");
        return false;
    }

    protected static async Task<bool> SetUserContextFromAuthTokenFileAsync(
        IAuthenticationService authenticationService
    )
    {
        var authFilePath = ConfigurationProvider.AuthTokenFilePath;

        try
        {
            if (!File.Exists(authFilePath))
            {
                Error($"Auth token file not found: {authFilePath}");
                Info("Run 'auth signin <request.json>' to sign in first.");
                return false;
            }

            var fileContent = await File.ReadAllTextAsync(authFilePath);
            if (string.IsNullOrWhiteSpace(fileContent))
            {
                Error($"Auth token file is empty: {authFilePath}");
                return false;
            }

            var jsonDoc = JsonDocument.Parse(fileContent);
            if (!jsonDoc.RootElement.TryGetProperty("AuthToken", out var authTokenElement))
            {
                Error($"Auth token file does not contain 'AuthToken' property: {authFilePath}");
                return false;
            }

            var authToken = authTokenElement.GetString();
            if (string.IsNullOrEmpty(authToken))
            {
                Error($"Auth token is empty in file: {authFilePath}");
                return false;
            }

            var setContextResult = await authenticationService.AuthenticateWithTokenAsync(
                authToken
            );
            if (setContextResult != UserAuthTokenValidationResultCode.Success)
            {
                Error($"Failed to set user context: {setContextResult}");
                Info(
                    "Your auth token may have expired. Run 'auth signin <request.json>' to sign in again."
                );
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Error($"Failed to load auth token from file: {ex.Message}");
            return false;
        }
    }
}
