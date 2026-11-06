using Corely.IAM.CompiledModel.MySql.Generated;

namespace Corely.IAM.CompiledModel.MySql;

public static class IAMOptionsExtensions
{
    extension(IAMOptions options)
    {
        public IAMOptions UseMySqlCompiledModel()
        {
            ArgumentNullException.ThrowIfNull(options);
            var iamVersion = typeof(IAMOptions).Assembly.GetName().Version;
            var modelVersion = typeof(IAMOptionsExtensions).Assembly.GetName().Version;
            if (iamVersion != modelVersion)
            {
                throw new InvalidOperationException(
                    $"Corely.IAM.CompiledModel.MySql {modelVersion} was built for Corely.IAM {modelVersion}, but Corely.IAM {iamVersion} is loaded. Use the same version of both."
                );
            }
            return options.UseCompiledModel(IamDbContextModel.Instance);
        }
    }
}
