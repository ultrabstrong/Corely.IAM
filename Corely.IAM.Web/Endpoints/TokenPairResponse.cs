namespace Corely.IAM.Web.Endpoints;

internal record TokenPairResponse(
    string AuthToken,
    string RefreshToken,
    DateTime RefreshExpiresUtc
);
