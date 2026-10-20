using System.Runtime.CompilerServices;
using Corely.IAM.UnitTests.PublicSurface;
using Corely.IAM.Web.Components.Shared;
using Corely.IAM.Web.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Corely.IAM.Web.UnitTests.PublicSurface;

public class PublicSurfaceTests
{
    private const string FIX =
        "Make it internal, or add it to the allowed list with the reason a host needs it by name.";

    private const string RAZOR_IMPORTS = "_Imports";

    private static readonly System.Reflection.Assembly _assembly = typeof(AppRoutes).Assembly;

    private static readonly Type[] _allowed = [typeof(AppRoutes)];

    [Fact]
    public void NoEntityProcessorMapperOrRepo_IsPublic()
    {
        var leaked = PublicTypes.InternalByNaming(_assembly);

        Assert.True(leaked.Count == 0, $"{Names(leaked)}. {FIX}");
    }

    [Fact]
    public void EveryPublicType_IsReachableFromThePublicApi()
    {
        var unreachable = PublicTypes.Unreachable(_assembly, IsRoot, _allowed);

        Assert.True(unreachable.Count == 0, $"{Names(unreachable)}. {FIX}");
    }

    [Fact]
    public void RegistrationComponentsAndLinks_AreRoots()
    {
        Assert.True(IsRoot(typeof(IamWebServiceExtensions)));
        Assert.True(IsRoot(typeof(EncryptionSigningPanel)));
        Assert.True(IsRoot(typeof(AccountScope)));
        Assert.True(IsRoot(typeof(IIamWebLinks)));
    }

    private static bool IsRoot(Type type) =>
        type.IsInterface
        || type.Name.EndsWith("Options")
        || type.Name == RAZOR_IMPORTS
        || typeof(IComponent).IsAssignableFrom(type)
        || typeof(PageModel).IsAssignableFrom(type)
        || (type.IsAbstract && type.IsSealed && type.IsDefined(typeof(ExtensionAttribute), false));

    private static string Names(IEnumerable<Type> types) =>
        string.Join(", ", types.Select(t => t.FullName));
}
