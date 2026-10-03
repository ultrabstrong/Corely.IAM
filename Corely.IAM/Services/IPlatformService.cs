using Corely.IAM.Platform.Models;

namespace Corely.IAM.Services;

public interface IPlatformService
{
    Task<BootstrapPlatformResult> BootstrapPlatformAsync(BootstrapPlatformRequest request);
    Task<int> CompletePlatformOwnerPermissionsAsync();
}
