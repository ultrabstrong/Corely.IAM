using Corely.IAM.CompiledModel.MsSql.Generated;

namespace Corely.IAM.CompiledModel.MsSql;

public static class IAMOptionsExtensions
{
    extension(IAMOptions options)
    {
        public IAMOptions UseMsSqlCompiledModel()
        {
            ArgumentNullException.ThrowIfNull(options);
            var iamVersion = typeof(IAMOptions).Assembly.GetName().Version;
            var modelVersion = typeof(IAMOptionsExtensions).Assembly.GetName().Version;
            if (iamVersion != modelVersion)
            {
                throw new InvalidOperationException(
                    $"Corely.IAM.CompiledModel.MsSql {modelVersion} was built for Corely.IAM {modelVersion}, but Corely.IAM {iamVersion} is loaded. Use the same version of both."
                );
            }
            return options.UseCompiledModel(IamDbContextModel.Instance);
        }
    }
}
