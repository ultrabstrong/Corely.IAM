using Corely.IAM.Models;

namespace Corely.IAM.UnitTests.Models;

public class ListResultTests
{
    [Fact]
    public void ToRetrieveListResult_CarriesEveryField()
    {
        var page = PagedResult<int>.Create([1, 2], 2, 0, 10);
        var result = new ListResult<int>(RetrieveResultCode.NotFoundError, "message", page);

        var retrieved = result.ToRetrieveListResult();

        Assert.Equal(RetrieveResultCode.NotFoundError, retrieved.ResultCode);
        Assert.Equal("message", retrieved.Message);
        Assert.Same(page, retrieved.Data);
    }
}
