using Corely.IAM.Models;

namespace Corely.IAM.Services;

public interface IRetrievalService
{
    Task<RetrieveAccountsResult> RetrieveAccountsAsync(RetrieveAccountsRequest request);
}
