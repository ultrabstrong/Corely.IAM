using Corely.Common.Extensions;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Extensions;
using Corely.IAM.Groups.Models;
using Corely.IAM.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Users.Models;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Services;

internal class ModificationServiceTelemetryDecorator(
    IModificationService inner,
    ILogger<ModificationServiceTelemetryDecorator> logger
) : IModificationService
{
    private readonly IModificationService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly ILogger<ModificationServiceTelemetryDecorator> _logger = logger.ThrowIfNull(
        nameof(logger)
    );

    public async Task<ModifyResult> ModifyAccountAsync(UpdateAccountRequest request) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(ModificationService),
            request,
            () => _inner.ModifyAccountAsync(request),
            logResult: true
        );

    public async Task<ModifyResult> ModifyUserAsync(UpdateUserRequest request) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(ModificationService),
            request,
            () => _inner.ModifyUserAsync(request),
            logResult: true
        );

    public async Task<ModifyResult> ModifyGroupAsync(UpdateGroupRequest request) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(ModificationService),
            request,
            () => _inner.ModifyGroupAsync(request),
            logResult: true
        );

    public async Task<ModifyResult> ModifyRoleAsync(UpdateRoleRequest request) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(ModificationService),
            request,
            () => _inner.ModifyRoleAsync(request),
            logResult: true
        );
}
