// This file is used by Code Analysis to maintain SuppressMessage attributes that are applied to this project.
// Project-level suppressions either have no target or are given a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// Suppresses the CA1416 warning about platform-specific API usage, ensuring that the code does not trigger warnings when analyzed by static code analysis tools.
// Use this only when the platform dependency is known and intended.
[assembly: SuppressMessage ("Interoperability", "CA1416")]
