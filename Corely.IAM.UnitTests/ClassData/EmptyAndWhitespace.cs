using System.Collections;

namespace Corely.IAM.UnitTests.ClassData;

public class EmptyAndWhitespace : IEnumerable<object[]>
{
    private readonly List<object[]> _data =
    [
        [string.Empty],
        [" "],
    ];

    public IEnumerator<object[]> GetEnumerator() => _data.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
