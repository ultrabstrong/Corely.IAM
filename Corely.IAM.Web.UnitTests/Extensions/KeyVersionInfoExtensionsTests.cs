using Corely.IAM.Security.Models;
using Corely.IAM.Web.Extensions;

namespace Corely.IAM.Web.UnitTests.Extensions;

public class KeyVersionInfoExtensionsTests
{
    [Theory]
    [InlineData(3, false, "Version 3 (current)")]
    [InlineData(2, false, "Version 2")]
    [InlineData(1, true, "Version 1 (compromised)")]
    public void OptionLabel_NamesWhatTheVersionIs_ForEachKindOfVersion(
        int version,
        bool isCompromised,
        string expected
    ) =>
        Assert.Equal(
            expected,
            new KeyVersionInfo(version, "RSA", null, isCompromised).OptionLabel(3)
        );
}
