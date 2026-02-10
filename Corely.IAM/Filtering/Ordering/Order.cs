namespace Corely.IAM.Filtering.Ordering;

public static class Order
{
    public static OrderBuilder<T> For<T>() => new();
}
