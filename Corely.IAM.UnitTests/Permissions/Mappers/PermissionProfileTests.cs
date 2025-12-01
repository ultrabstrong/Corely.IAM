using Corely.IAM.Permissions.Entities;
using Corely.IAM.Permissions.Models;
using Corely.IAM.UnitTests.Mappers.AutoMapper;

namespace Corely.IAM.UnitTests.Permissions.Mappers;

public class PermissionProfileTests : BidirectionalProfileDelegateTestsBase
{
    private class Delegate : BidirectionalProfileTestsBase<Permission, PermissionEntity>;

    protected override BidirectionalProfileTestsBase GetDelegate() => new Delegate();
}
