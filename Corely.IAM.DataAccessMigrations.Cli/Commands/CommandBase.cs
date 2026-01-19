using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.NamingConventionBinder;
using System.Reflection;
using Corely.IAM.DataAccessMigrations.Cli.Attributes;

namespace Corely.IAM.DataAccessMigrations.Cli.Commands;

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

        argument = null!;
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

        option = null!;
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

    protected void ShowHelp(string? message = null)
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

    protected static bool ValidateSettings(out DatabaseConnectionValidator.ValidationResult result)
    {
        result = DatabaseConnectionValidator.ValidateSettingsFile();
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

    protected static async Task<bool> ValidateConnectionAsync(IServiceProvider serviceProvider)
    {
        var result = await DatabaseConnectionValidator.ValidateConnectionAsync(serviceProvider);
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
}
