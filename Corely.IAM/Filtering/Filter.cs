namespace Corely.IAM.Filtering;

public static class Filter
{
    public static FilterBuilder<T> For<T>() => new();
}
