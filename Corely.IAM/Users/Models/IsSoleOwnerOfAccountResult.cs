namespace Corely.IAM.Users.Models;

/// <summary>
/// Result of checking if a user is the sole owner of an account.
/// </summary>
/// <param name="IsSoleOwner">True if the user is the only owner of the account</param>
/// <param name="UserHasOwnerRole">True if the user has the owner role (directly or via group)</param>
/// <param name="HasSingleOwnershipSource">True if the user's ownership comes from exactly one source
/// (either one direct assignment OR one group). False if user has ownership via multiple sources
/// (e.g., both direct and via group, or via multiple groups)</param>
public record IsSoleOwnerOfAccountResult(
    bool IsSoleOwner,
    bool UserHasOwnerRole,
    bool HasSingleOwnershipSource = true
);
