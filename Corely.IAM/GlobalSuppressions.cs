// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Style",
    "IDE0290:Use primary constructor",
    Justification = "Primary constructors don't support making params readonly (as of 12-30-23)",
    Scope = "module"
)]
[assembly: SuppressMessage(
    "Performance",
    "CA1873:Avoid potentially expensive logging",
    Justification = "Explicit IsEnabled guards around structured logging add noise without enough value here.",
    Scope = "module"
)]
