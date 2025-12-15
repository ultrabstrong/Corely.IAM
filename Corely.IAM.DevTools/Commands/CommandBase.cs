using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.NamingConventionBinder;
using System.Reflection;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.DevTools.Commands;

internal abstract class CommandBase : Command
{
    private const string _helpFlag = "--help";

    private readonly Dictionary<string, Argument> _arguments = [];
    private readonly Dictionary<string, Option> _options = [];

    protected CommandBase(string name, string description, string additionalDescription)
        : this(name, $"{description}{Environment.NewLine}{additionalDescription}") { }

    protected CommandBase(string name, string description)
        : base(name, description)
    {
        var type = GetType();
        foreach (
            var property in type.GetProperties(
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly
            )
        )
        {
            var optionAttribute = property.GetCustomAttribute<OptionAttribute>();
            if (optionAttribute == null)
            {
                var argumentAttribute = property.GetCustomAttribute<ArgumentAttribute>();
                if (CreateArgument(property, argumentAttribute, out var argument))
                {
                    _arguments.Add(type.FullName + property.Name, argument);
                    AddArgument(argument);
                }
            }
            else
            {
                if (CreateOption(property, optionAttribute, out var option))
                {
                    _options.Add(type.FullName + property.Name, option);
                    AddOption(option);
                }
            }
        }

        Handler = CommandHandler.Create(InvokeExecute);
    }

    private bool CreateArgument(
        PropertyInfo property,
        ArgumentAttribute? argumentAttribute,
        out Argument argument
    )
    {
        var argumentGenericType = typeof(Argument<>).MakeGenericType(property.PropertyType);
        var optionalText = argumentAttribute?.IsRequired ?? false ? string.Empty : "[Optional] ";

        var argumentInstance = Activator.CreateInstance(
            argumentGenericType,
            [property.Name, $"{optionalText}{argumentAttribute?.Description}"]
        );

        if (argumentInstance is Argument arg)
        {
            if (argumentAttribute != null)
            {
                if (argumentAttribute.ArgumentArity != null)
                {
                    arg.Arity = argumentAttribute.ArgumentArity.Value;
                }
                if (!argumentAttribute.IsRequired)
                {
                    arg.SetDefaultValue(property.GetValue(this));
                }
            }

            argument = arg;
            return true;
        }

        argument = null;
        return false;
    }

    private bool CreateOption(
        PropertyInfo property,
        OptionAttribute optionAttribute,
        out Option option
    )
    {
        var optionGenericType = typeof(Option<>).MakeGenericType(property.PropertyType);
        var optionInstance = Activator.CreateInstance(
            optionGenericType,
            [optionAttribute.Aliases, optionAttribute.Description]
        );

        if (optionInstance is Option opt)
        {
            if (optionAttribute.ArgumentArity != null)
            {
                opt.Arity = optionAttribute.ArgumentArity.Value;
            }
            opt.SetDefaultValue(property.GetValue(this));

            option = opt;
            return true;
        }

        option = null;
        return false;
    }

    private async Task InvokeExecute(BindingContext context)
    {
        var type = GetType();
        foreach (
            var property in type.GetProperties(
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly
            )
        )
        {
            var value = _options.TryGetValue(type.FullName + property.Name, out Option? option)
                ? context.ParseResult.GetValueForOption(option)
                : context.ParseResult.GetValueForArgument(
                    _arguments[type.FullName + property.Name]
                );

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

    protected virtual Task ExecuteAsync()
    {
        Execute();
        return Task.CompletedTask;
    }

    protected virtual void Execute() { }

    protected void ShowHelp(string message = null)
    {
        if (!string.IsNullOrEmpty(message))
        {
            Warn(message);
            Console.WriteLine();
        }
        this.Invoke(_helpFlag);
    }

    protected static void Success(string message)
    {
        Console.WriteLine(message, ConsoleColor.Green);
    }

    protected static void Success(IEnumerable<string> messages)
    {
        WriteColored(messages, ConsoleColor.Green);
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
        Console.WriteLine(message, ConsoleColor.Red);
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

    protected static async Task<bool> SetUserContextFromAuthTokenFileAsync(
        string authFilePath,
        IUserContextProvider userContextProvider
    )
    {
        try
        {
            if (!File.Exists(authFilePath))
            {
                Error($"Auth token file not found: {authFilePath}");
                return false;
            }

            var fileContent = await File.ReadAllTextAsync(authFilePath);
            if (string.IsNullOrWhiteSpace(fileContent))
            {
                Error($"Auth token file is empty: {authFilePath}");
                return false;
            }

            // The file should contain a JSON object with an AuthToken property
            var jsonDoc = System.Text.Json.JsonDocument.Parse(fileContent);
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

            var setContextResult = await userContextProvider.SetUserContextAsync(authToken);
            if (setContextResult != UserAuthTokenValidationResultCode.Success)
            {
                Error($"Failed to set user context: {setContextResult}");
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
