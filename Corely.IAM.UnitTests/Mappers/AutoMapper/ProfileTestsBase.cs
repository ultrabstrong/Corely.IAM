using AutoFixture;
using AutoMapper;

namespace Corely.IAM.UnitTests.Mappers.AutoMapper;

public abstract class ProfileTestsBase
{
    [Fact]
    public abstract void Map_MapsSourceToDestination();

    protected static T GetMock<T>(object[] args)
        where T : class
    {
        try
        {
            return new Mock<T>(args).Object;
        }
        catch (Exception)
        {
            return new Fixture().Create<T>();
        }
    }
}

public abstract class ProfileDelegateTestsBase : ProfileTestsBase
{
    protected abstract ProfileTestsBase GetDelegate();

    [Fact]
    public override void Map_MapsSourceToDestination() =>
        GetDelegate().Map_MapsSourceToDestination();
}

public abstract class ProfileTestsBase<TSource, TDestination> : ProfileTestsBase
    where TSource : class
    where TDestination : class
{
    protected readonly IMapper mapper;
    private readonly ServiceFactory _serviceFactory = new();

    protected ProfileTestsBase()
    {
        mapper = _serviceFactory.GetRequiredService<IMapper>();
    }

    [Fact]
    public override void Map_MapsSourceToDestination()
    {
        var source = GetSource();
        var modifiedSource = ApplySourceModifications(source);
        mapper.Map<TDestination>(modifiedSource);
    }

    protected virtual TSource GetSource() => GetMock<TSource>(GetSourceParams());

    protected virtual TSource ApplySourceModifications(TSource source) => source;

    protected virtual object[] GetSourceParams() => [];
}
