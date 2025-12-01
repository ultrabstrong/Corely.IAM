using System.Collections;

namespace Corely.IAM.UnitTests.ClassData;

public class NullEmptyAndWhitespace : IEnumerable<object[]>
{
    private readonly List<object[]> _data =
    [
        [null],
        [string.Empty],
        [" "],
    ];

    public IEnumerator<object[]> GetEnumerator() => _data.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
