using AutoMapper;

namespace Corely.IAM.UnitTests.Mappers.AutoMapper;

public abstract class BidirectionalProfileTestsBase : ProfileTestsBase
{
    [Fact]
    public abstract void ReverseMap_MapsDestinationToSource();
}

public abstract class BidirectionalProfileDelegateTestsBase : BidirectionalProfileTestsBase
{
    protected abstract BidirectionalProfileTestsBase GetDelegate();

    [Fact]
    public override void Map_MapsSourceToDestination() =>
        GetDelegate().Map_MapsSourceToDestination();

    [Fact]
    public override void ReverseMap_MapsDestinationToSource() =>
        GetDelegate().ReverseMap_MapsDestinationToSource();
}

public abstract class BidirectionalProfileTestsBase<TSource, TDestination>
    : BidirectionalProfileTestsBase
    where TSource : class
    where TDestination : class
{
    protected readonly IMapper mapper;
    private readonly ServiceFactory _serviceFactory = new();

    protected BidirectionalProfileTestsBase()
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

    protected virtual object[] GetSourceParams() => [];

    protected virtual TSource ApplySourceModifications(TSource source) => source;

    [Fact]
    public override void ReverseMap_MapsDestinationToSource()
    {
        var destination = GetDestination();
        var modifiedDestination = ApplyDestinatonModifications(destination);
        mapper.Map<TSource>(modifiedDestination);
    }

    protected virtual TDestination GetDestination() =>
        GetMock<TDestination>(GetDestinationParams());

    protected virtual object[] GetDestinationParams() => [];

    protected virtual TDestination ApplyDestinatonModifications(TDestination destination) =>
        destination;
}
