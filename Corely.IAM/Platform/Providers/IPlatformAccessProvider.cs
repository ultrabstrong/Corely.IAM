namespace Corely.IAM.Platform.Providers;

internal interface IPlatformAccessProvider
{
    Task<Guid?> GetPlatformAccountIdAsync();
    Task<bool> CanEnterAnyAccountAsync(Guid userId);
}
