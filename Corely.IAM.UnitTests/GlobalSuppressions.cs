// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// module
[assembly: SuppressMessage(
    "Usage",
    "xUnit1042:The member referenced by the MemberData attribute returns untyped data rows",
    Justification = "Using typed TheoryData instead of a generic object[] is overkill for testing",
    Scope = "module"
)]
[assembly: SuppressMessage(
    "Style",
    "IDE0290:Use primary constructor",
    Justification = "Primary constructors don't support making params readonly (as of 12-30-23)",
    Scope = "module"
)]
