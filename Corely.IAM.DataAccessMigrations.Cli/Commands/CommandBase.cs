using System.CommandLine;
using System.Reflection;
using Corely.IAM.DataAccessMigrations.Cli.Attributes;

namespace Corely.IAM.DataAccessMigrations.Cli.Commands;

internal abstract class CommandBase : Command
{
    private const string _helpFlag = "--help";

    // Property name -> the parse-result name of the symbol bound to it. Options are looked up by
    // their primary alias, arguments by the property name.
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

        // The name is the only constructor parameter now; description is a property.
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
            // beta4 inferred optionality from the presence of a default value, including a null
            // one. 2.0 takes it from arity alone, so an optional argument has to say so.
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

        // Option<T> now takes a mandatory name plus a params array of aliases. The longest alias
        // is used as the name so help renders "--verbose" rather than "-v" as the primary form.
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

    // DefaultValueFactory is Func<ArgumentResult, T>, so the delegate has to be built against the
    // property's own type rather than object.
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

        // Showing help re-invokes this command. If the help option is not reachable - a command
        // parsed on its own rather than through the root command - that lands back in the action
        // that failed, which calls ShowHelp again. Guard so the second attempt stops instead of
        // recursing until the stack runs out.
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
