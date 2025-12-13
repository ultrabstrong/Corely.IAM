using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.DevTools.Attributes;
using Corely.IAM.Services;
using Corely.IAM.Validators;

namespace Corely.IAM.DevTools.Commands.Deregistration;

internal partial class Deregistration : CommandBase
{
    internal class DeregisterAccount : CommandBase
    {
        private readonly IDeregistrationService _deregistrationService;

        public DeregisterAccount(IDeregistrationService deregistrationService)
            : base("account", "Deregister the currently signed-in account")
        {
            _deregistrationService = deregistrationService.ThrowIfNull(
                nameof(deregistrationService)
            );
        }

        protected override async Task ExecuteAsync()
        {
            try
            {
                var result = await _deregistrationService.DeregisterAccountAsync();
                Console.WriteLine(JsonSerializer.Serialize(result));
            }
            catch (ValidationException ex)
            {
                Error(ex.ValidationResult!.Errors!.Select(e => e.Message));
            }
        }
    }
}
