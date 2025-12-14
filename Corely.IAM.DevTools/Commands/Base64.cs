using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;

namespace Corely.IAM.DevTools.Commands;

internal class Base64 : CommandBase
{
    [Option("-e", "--encode", Description = "Value to encode")]
    private string Encode { get; init; } = null!;

    [Option("-d", "--decode", Description = "Value to decode")]
    private string Decode { get; init; } = null!;

    public Base64()
        : base("base64", "Base64 operations") { }

    protected override void Execute()
    {
        if (!string.IsNullOrEmpty(Encode))
        {
            Console.WriteLine(Encode.Base64Encode());
        }
        if (!string.IsNullOrEmpty(Decode))
        {
            Console.WriteLine(Decode.Base64Decode());
        }
        if (string.IsNullOrEmpty(Encode) && string.IsNullOrEmpty(Decode))
        {
            Console.WriteLine("No value to encode or decode");
        }
    }
}
