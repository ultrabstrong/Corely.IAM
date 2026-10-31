using System.Net;
using Corely.IAM.Models;
using Corely.IAM.Security.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;
using Corely.IAM.Web.Configuration;
using Corely.IAM.Web.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Corely.IAM.Web.Extensions;

public static class IamWebEndpointExtensions
{
    public const string TOKEN_ENDPOINTS_TAG = "IAM tokens";
    public const string JWKS_ENDPOINTS_TAG = "IAM keys";
    private const string JWK_SET_CONTENT_TYPE = "application/jwk-set+json";
    private const string JWKS_CACHE_CONTROL = "public, max-age=300";
    private const string REFUSED_KEY = "The key was refused.";
    private const string SESSION_ENDED = "The session has ended. Sign in again.";

    extension(IEndpointRouteBuilder endpoints)
    {
        public RouteGroupBuilder MapIAMTokenEndpoints(string prefix = "/api/auth")
        {
            if (
                !endpoints
                    .ServiceProvider.GetRequiredService<IOptions<IAMWebOptions>>()
                    .Value.AcceptBearerTokens
            )
                throw new InvalidOperationException(
                    $"{nameof(MapIAMTokenEndpoints)} needs bearer tokens turned on. Add "
                        + $"options.{nameof(IAMWebOptions.AcceptBearerTokens)} = true to AddIAMWeb(options => ...). "
                        + "Without it, every API call made with a token these endpoints issue is refused with 401."
                );

            var group = endpoints.MapGroup(prefix).WithTags(TOKEN_ENDPOINTS_TAG).AsIAMApi();

            group
                .MapPost("/apikey", SignInWithApiKeyAsync)
                .WithName("IamSignInWithApiKey")
                .Produces<ApiResponse<TokenPairResponse>>()
                .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
                .WithSummary("Exchange an API key for an access token and a refresh token")
                .WithDescription(
                    "Send the key as ciam_<prefix>_<secret>. Use the access token as a Bearer token on every call. "
                        + "Every refusal returns 401 with no detail, whatever the reason."
                );

            group
                .MapPost("/renew", RenewAsync)
                .WithName("IamRenewToken")
                .Produces<ApiResponse<TokenPairResponse>>()
                .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
                .Produces<ApiResponse<object>>(StatusCodes.Status409Conflict)
                .WithSummary("Trade a refresh token for a new pair")
                .WithDescription(
                    "Each refresh token works once. 409 means another call of the same client renewed first: keep the pair "
                        + "that call received. 401 means the session has ended: sign in again. Reusing a spent refresh token "
                        + "after a short grace period ends the whole session."
                );

            group
                .MapPost("/signout", SignOutAsync)
                .WithName("IamSignOut")
                .Produces<ApiResponse<object>>()
                .Produces<ApiResponse<object>>(StatusCodes.Status401Unauthorized)
                .WithSummary("End the session of the Bearer token on this request")
                .WithDescription("Requires the Authorization header. Cookies are not accepted.")
                .RequireAuthorization();

            return group;
        }

        public RouteGroupBuilder MapIAMJwksEndpoints(string prefix = "/api/accounts")
        {
            var group = endpoints.MapGroup(prefix).WithTags(JWKS_ENDPOINTS_TAG).AllowAnonymous();

            group
                .MapGet("/{accountId:guid}/.well-known/jwks.json", GetAccountJwksAsync)
                .WithName("IamAccountJwks")
                .Produces<AccountJsonWebKeySet>(StatusCodes.Status200OK, JWK_SET_CONTENT_TYPE)
                .Produces(StatusCodes.Status404NotFound)
                .WithSummary("The account's public keys as a JSON Web Key Set")
                .WithDescription(
                    "Needs no sign in. Lists every signing key version, newest first, so signatures made with "
                        + "an earlier key still verify, and the current encryption key. Fetch again when a kid is unknown."
                );

            return group;
        }
    }

    extension<TBuilder>(TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        public TBuilder AsIAMApi() => builder.WithMetadata(IamApiEndpointMetadata.Instance);
    }

    private static async Task<IResult> GetAccountJwksAsync(
        Guid accountId,
        HttpContext context,
        IRetrievalService retrievalService
    )
    {
        var result = await retrievalService.GetAccountJsonWebKeySetAsync(accountId);
        if (result.ResultCode != RetrieveResultCode.Success || result.Item is null)
        {
            if (context.Features.Get<IStatusCodePagesFeature>() is { } statusCodePages)
                statusCodePages.Enabled = false;
            return TypedResults.NotFound();
        }

        context.Response.Headers.CacheControl = JWKS_CACHE_CONTROL;
        return TypedResults.Json(result.Item, contentType: JWK_SET_CONTENT_TYPE);
    }

    private static async Task<IResult> SignInWithApiKeyAsync(
        ApiKeySignInBody body,
        IAuthenticationService authenticationService
    )
    {
        if (string.IsNullOrWhiteSpace(body.ApiKey))
            return Refused(REFUSED_KEY);

        var result = await authenticationService.SignInWithApiKeyAsync(
            new SignInWithApiKeyRequest(
                body.ApiKey,
                string.IsNullOrWhiteSpace(body.DeviceId)
                    ? ApiKeySignInBody.DEFAULT_DEVICE_ID
                    : body.DeviceId
            )
        );

        return result.ToTokenPairResponse() is { } pair
            ? TypedResults.Ok(ApiResponse.Success(pair))
            : Refused(REFUSED_KEY);
    }

    private static async Task<IResult> RenewAsync(
        RenewTokenBody body,
        IAuthenticationService authenticationService
    )
    {
        if (string.IsNullOrWhiteSpace(body.RefreshToken))
            return Refused(SESSION_ENDED);

        var result = await authenticationService.RenewAuthTokenAsync(
            new RenewAuthTokenRequest(body.RefreshToken)
        );

        if (result.ResultCode == RenewAuthTokenResultCode.RefreshTokenAlreadyRotatedError)
            return TypedResults.Json(
                ApiResponse.Failure(
                    "AlreadyRenewed",
                    "Another call renewed first. Keep the pair that call received."
                ),
                statusCode: StatusCodes.Status409Conflict
            );

        return result.ToTokenPairResponse() is { } pair
            ? TypedResults.Ok(ApiResponse.Success(pair))
            : Refused(SESSION_ENDED);
    }

    private static async Task<IResult> SignOutAsync(
        HttpRequest request,
        IAuthenticationService authenticationService,
        IUserContextProvider userContextProvider
    )
    {
        if (
            !request.HasBearerToken()
            || userContextProvider.GetUserContext()?.AuthTokenId is not { } tokenId
        )
            return Refused(SESSION_ENDED);

        return await authenticationService.SignOutAsync(new SignOutRequest(tokenId.ToString()))
            ? TypedResults.Ok(ApiResponse.Success())
            : Refused(SESSION_ENDED);
    }

    private static IResult Refused(string message) =>
        TypedResults.Json(
            ApiResponse.Failure(nameof(HttpStatusCode.Unauthorized), message),
            statusCode: StatusCodes.Status401Unauthorized
        );
}
