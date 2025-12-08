using Corely.Common.Extensions;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Permissions.Processors;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Services;

internal class DeregistrationService : IDeregistrationService
{
    private readonly ILogger<DeregistrationService> _logger;
    private readonly IPermissionProcessor _permissionProcessor;

    public DeregistrationService(
        ILogger<DeregistrationService> logger,
        IPermissionProcessor permissionProcessor
    )
    {
        _logger = logger.ThrowIfNull(nameof(logger));
        _permissionProcessor = permissionProcessor.ThrowIfNull(nameof(permissionProcessor));
    }

    public async Task<DeregisterPermissionResult> DeregisterPermissionAsync(
        DeregisterPermissionRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        _logger.LogInformation("Deregistering permission {PermissionId}", request.PermissionId);

        var result = await _permissionProcessor.DeletePermissionAsync(request.PermissionId);

        if (result.ResultCode != DeletePermissionResultCode.Success)
        {
            _logger.LogInformation(
                "Deregistering permission failed for permission id {PermissionId}",
                request.PermissionId
            );
            return new DeregisterPermissionResult(
                (DeregisterPermissionResultCode)result.ResultCode,
                result.Message
            );
        }

        _logger.LogInformation("Permission {PermissionId} deregistered", request.PermissionId);
        return new DeregisterPermissionResult(DeregisterPermissionResultCode.Success, string.Empty);
    }

    public Task<DeregisterUserResult> DegisterUserAsync(DeregisterUserRequest request)
    {
        _logger.LogDebug(
            "Need to implement permissions so we can check if the user is the owner of any account"
        );
        throw new NotImplementedException();
    }

    public Task<DeregisterAccountResult> DegisterAccountAsync(DeregisterAccountRequest request)
    {
        _logger.LogDebug(
            "Need to implement permissions so we can check if the user is the owner of this account"
        );
        throw new NotImplementedException();
    }
}
