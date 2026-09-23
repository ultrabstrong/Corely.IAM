using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Style",
    "IDE0290:Use primary constructor",
    Justification = "Primary constructors don't support making params readonly (as of 12-30-23)"
)]
[assembly: SuppressMessage(
    "Performance",
    "CA1869:Cache and reuse 'JsonSerializerOptions' instances",
    Justification = "This doesn't seem worth the small performance improvement"
)]
