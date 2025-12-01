using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;

namespace Corely.IAM.DevTools.Commands;

internal class Url : CommandBase
{
    [Option("-e", "--encode", Description = "Value to encode")]
    private string Encode { get; init; } = null!;

    [Option("-d", "--decode", Description = "Value to decode")]
    private string Decode { get; init; } = null!;

    public Url()
        : base("url", "Url encode/decode operations") { }

    protected override void Execute()
    {
        if (!string.IsNullOrEmpty(Encode))
        {
            Console.WriteLine(Encode.UrlEncode());
        }
        if (!string.IsNullOrEmpty(Decode))
        {
            Console.WriteLine(Decode.UrlDecode());
        }
        if (string.IsNullOrEmpty(Encode) && string.IsNullOrEmpty(Decode))
        {
            Console.WriteLine("No value to encode or decode");
        }
    }
}
