using Corely.IAM.Security.Entities;
using Corely.IAM.Security.Enums;

namespace Corely.IAM.Security.Models;

internal sealed record SessionTokenRenewal(SessionTokenRenewalCode Code, AuthTokenEntity? Token);
