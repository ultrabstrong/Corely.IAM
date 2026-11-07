using Corely.IAM.Models;

namespace Corely.IAM.IdentityProviders.Models;

public record CountSoleSignInUsersResult(RetrieveResultCode ResultCode, string Message, int Count);
