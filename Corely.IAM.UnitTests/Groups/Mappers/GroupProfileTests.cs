using Corely.IAM.Groups.Entities;
using Corely.IAM.Groups.Models;
using Corely.IAM.UnitTests.Mappers.AutoMapper;

namespace Corely.IAM.UnitTests.Groups.Mappers;

public class GroupProfileTests : BidirectionalProfileDelegateTestsBase
{
    private class Delegate : BidirectionalProfileTestsBase<Group, GroupEntity>;

    protected override BidirectionalProfileTestsBase GetDelegate() => new Delegate();
}
