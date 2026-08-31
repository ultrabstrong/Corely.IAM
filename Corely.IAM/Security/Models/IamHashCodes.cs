namespace Corely.IAM.Security.Models;

/// <summary>
/// The hash providers this library uses, kept separate because passwords and generated secrets
/// have opposite requirements.
///
/// A password is low-entropy and attacked offline, so it needs a deliberately slow hash. A
/// recovery token is high-entropy random data, so a slow hash adds latency to every issue and
/// validation while adding nothing an attacker has to defeat.
/// </summary>
internal sealed record IamHashCodes(string PasswordHashCode, string TokenHashCode);
