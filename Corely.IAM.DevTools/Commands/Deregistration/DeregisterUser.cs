using System.Text.Json;
using Corely.Common.Extensions;
using Corely.IAM.Services;
using Corely.IAM.Validators;

namespace Corely.IAM.DevTools.Commands.Deregistration;

internal partial class Deregistration : CommandBase
{
    internal class DeregisterUser : CommandBase
    {
        private readonly IDeregistrationService _deregistrationService;

        public DeregisterUser(IDeregistrationService deregistrationService)
            : base("user", "Deregister a user")
        {
            _deregistrationService = deregistrationService.ThrowIfNull(
                nameof(deregistrationService)
            );
        }

        protected override async Task ExecuteAsync()
        {
            try
            {
                var result = await _deregistrationService.DeregisterUserAsync();
                Console.WriteLine(JsonSerializer.Serialize(result));
            }
            catch (ValidationException ex)
            {
                Error(ex.ValidationResult!.Errors!.Select(e => e.Message));
            }
        }
    }
}
