using Corely.IAM.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Services;
using Corely.IAM.Users.Providers;
using Corely.IAM.Web.Endpoints;
using Corely.IAM.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.OpenApi;

namespace Corely.IAM.WebApp.Api;

internal static class WebAppApiExtensions
{
    public const string API_PREFIX = "/api";
    public const string AUTH_PREFIX = "/api/auth";
    public const string OPENAPI_PATH = "/openapi/v1.json";
    private const string BEARER_SCHEME = "Bearer";

    extension(IServiceCollection services)
    {
        public IServiceCollection AddWebAppApi() =>
            services.AddOpenApi(options =>
                options
                    .AddDocumentTransformer(
                        (document, _, _) =>
                        {
                            document.Info.Title = "Corely.IAM WebApp API";
                            document.Info.Description =
                                "Exchange an API key at POST /api/auth/apikey, send the access token as "
                                + "'Authorization: Bearer <token>', and renew at POST /api/auth/renew before it expires.";
                            document.Components ??= new OpenApiComponents();
                            document.Components.SecuritySchemes ??=
                                new Dictionary<string, IOpenApiSecurityScheme>();
                            document.Components.SecuritySchemes[BEARER_SCHEME] =
                                new OpenApiSecurityScheme
                                {
                                    Type = SecuritySchemeType.Http,
                                    Scheme = "bearer",
                                    BearerFormat = "JWT",
                                };
                            return Task.CompletedTask;
                        }
                    )
                    .AddOperationTransformer(
                        (operation, context, _) =>
                        {
                            if (
                                context
                                    .Description.ActionDescriptor.EndpointMetadata.OfType<IAuthorizeData>()
                                    .Any()
                            )
                            {
                                operation.Security =
                                [
                                    new OpenApiSecurityRequirement
                                    {
                                        [
                                            new OpenApiSecuritySchemeReference(
                                                BEARER_SCHEME,
                                                context.Document
                                            )
                                        ] = [],
                                    },
                                ];
                            }
                            return Task.CompletedTask;
                        }
                    )
            );
    }

    extension(IEndpointRouteBuilder endpoints)
    {
        public RouteGroupBuilder MapWebAppApi()
        {
            var group = endpoints
                .MapGroup(API_PREFIX)
                .RequireAuthorization()
                .AsIAMApi()
                .WithTags("WebApp");

            group
                .MapGet("/me", GetCaller)
                .WithName("GetCaller")
                .WithSummary("The user or API key the token belongs to, and its account");

            group
                .MapGet("/roles", ListRolesAsync)
                .WithName("ListRoles")
                .WithSummary("The roles of the token's account the caller may read");

            return group;
        }
    }

    private static Results<Ok<ApiResponse<CallerSummary>>, UnauthorizedHttpResult> GetCaller(
        IUserContextProvider userContextProvider
    ) =>
        CallerSummary.From(userContextProvider.GetUserContext()) is { } caller
            ? TypedResults.Ok(ApiResponse.Success(caller))
            : TypedResults.Unauthorized();

    private static async Task<
        Results<Ok<ApiResponse<List<RoleSummary>>>, UnauthorizedHttpResult, ForbidHttpResult>
    > ListRolesAsync(
        IUserContextProvider userContextProvider,
        IRetrievalService retrievalService,
        int skip = 0,
        int take = 25
    )
    {
        if (userContextProvider.GetUserContext()?.CurrentAccount is not { } account)
            return TypedResults.Unauthorized();

        var result = await retrievalService.ListRolesAsync(
            new ListRolesRequest(account.Id, Skip: skip, Take: take)
        );

        return result.ResultCode == RetrieveResultCode.Success
            ? TypedResults.Ok(
                ApiResponse.Success(result.Data?.Items.Select(RoleSummary.From).ToList() ?? [])
            )
            : TypedResults.Forbid();
    }
}
