using Corely.IAM.Models;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Services;

internal class DeregistrationService : IDeregistrationService
{
    private readonly ILogger<DeregistrationService> _logger;

    public DeregistrationService(ILogger<DeregistrationService> logger)
    {
        _logger = logger;
    }

    public Task<DeregisterUserResult> DegisterUserAsync(DeregisterUserRequest request)
    {
        // Need to implement permissions so we can check if the user is the owner of any account
        _logger.LogDebug(
            "Need to implement permissions so we can check if the user is the owner of any account"
        );
        throw new NotImplementedException();
    }

    public Task<DeregisterAccountResult> DegisterAccountAsync(DeregisterAccountRequest request)
    {
        // Need to implement permissions so we can check if the user is the owner of this account
        _logger.LogDebug(
            "Need to implement permissions so we can check if the user is the owner of this account"
        );
        throw new NotImplementedException();
    }
}
