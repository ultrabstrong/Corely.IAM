using Corely.Common.Redaction;
using Serilog.Core;
using Serilog.Events;

namespace Corely.IAM.DevTools.SerilogCustomization;

public class SerilogRedactionEnricher(List<IRedactionProvider> redactors) : ILogEventEnricher
{
    private readonly List<IRedactionProvider> _redactors = redactors;

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        foreach (var property in logEvent.Properties.ToList())
        {
            var text = property.Value switch
            {
                ScalarValue { Value: string value } => value,
                ScalarValue => null,
                _ => property.Value.ToString(),
            };
            if (text is null)
                continue;

            var redacted = _redactors.Aggregate(
                text,
                (value, redactor) => redactor.Redact(value) ?? value
            );
            if (redacted != text)
            {
                logEvent.AddOrUpdateProperty(
                    new LogEventProperty(property.Key, new ScalarValue(redacted))
                );
            }
        }
    }
}
