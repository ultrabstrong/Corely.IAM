using Corely.Common.Providers.Redaction;
using Corely.Common.Redaction;
using Corely.IAM.DevTools.SerilogCustomization;
using Serilog.Events;

namespace Corely.IAM.DevTools.UnitTests;

public class SerilogRedactionEnricherTests
{
    private static LogEvent Event(params LogEventProperty[] properties) =>
        new(
            DateTimeOffset.UnixEpoch,
            LogEventLevel.Information,
            null,
            MessageTemplate.Empty,
            properties
        );

    private static object? Value(LogEvent logEvent, string name) =>
        ((ScalarValue)logEvent.Properties[name]).Value;

    [Fact]
    public void Enrich_LeavesText_WithNothingToRedact_AsItWas()
    {
        var logEvent = Event(new LogEventProperty("Name", new ScalarValue("abc")));

        new SerilogRedactionEnricher([new Replace("secret", "***")]).Enrich(logEvent, null!);

        Assert.Equal("abc", Value(logEvent, "Name"));
    }

    [Fact]
    public void Enrich_LeavesNumbers_AsNumbers()
    {
        var logEvent = Event(new LogEventProperty("Count", new ScalarValue(64)));

        new SerilogRedactionEnricher([new Replace("6", "*")]).Enrich(logEvent, null!);

        Assert.Equal(64, Value(logEvent, "Count"));
    }

    [Fact]
    public void Enrich_AppliesEveryRedactor_InTurn()
    {
        var logEvent = Event(new LogEventProperty("Text", new ScalarValue("user secret token")));

        new SerilogRedactionEnricher([
            new Replace("secret", "***"),
            new Replace("token", "###"),
        ]).Enrich(logEvent, null!);

        Assert.Equal("user *** ###", Value(logEvent, "Text"));
    }

    [Fact]
    public void Enrich_RedactsAPassword_InsideALoggedObject()
    {
        var logEvent = Event(
            new LogEventProperty(
                "Request",
                new StructureValue([
                    new LogEventProperty("Username", new ScalarValue("owner")),
                    new LogEventProperty("Password", new ScalarValue("hunter22")),
                ])
            )
        );

        new SerilogRedactionEnricher([new PasswordRedactionProvider()]).Enrich(logEvent, null!);

        var rendered = logEvent.Properties["Request"].ToString();
        Assert.DoesNotContain("hunter22", rendered);
        Assert.Contains("owner", rendered);
    }

    [Fact]
    public void Enrich_LeavesALoggedObject_Structured_WhenNothingIsRedacted()
    {
        var request = new StructureValue([
            new LogEventProperty("Username", new ScalarValue("owner")),
        ]);
        var logEvent = Event(new LogEventProperty("Request", request));

        new SerilogRedactionEnricher([new PasswordRedactionProvider()]).Enrich(logEvent, null!);

        Assert.Same(request, logEvent.Properties["Request"]);
    }

    private sealed class Replace(string find, string with) : IRedactionProvider
    {
        public string? Redact(string? input) => input?.Replace(find, with);
    }
}
