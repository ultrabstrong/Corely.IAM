using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Usage",
    "xUnit1042:The member referenced by the MemberData attribute returns untyped data rows",
    Justification = "Using typed TheoryData instead of a generic object[] is overkill for testing"
)]
[assembly: SuppressMessage(
    "Style",
    "IDE0290:Use primary constructor",
    Justification = "Primary constructors don't support making params readonly (as of 12-30-23)"
)]
[assembly: SuppressMessage(
    "Performance",
    "CA1861:Avoid constant arrays as arguments",
    Justification = "Using static list for tests makes tests more tightly coupled / harder to modify later"
)]
