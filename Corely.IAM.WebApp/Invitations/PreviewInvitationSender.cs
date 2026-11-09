using Corely.IAM.Invitations.Models;
using Corely.IAM.Invitations.Providers;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Options;

namespace Corely.IAM.WebApp.Invitations;

internal sealed class PreviewInvitationSender(
    InvitationPreviewStore store,
    IOptions<DemoFeaturesOptions> options,
    IServer server
) : IInvitationSender
{
    public Task<InvitationSendResult> SendAsync(InvitationDelivery delivery)
    {
        if (options.Value.InvitationPreview == InvitationPreviewMode.Fail)
            return Task.FromResult(
                new InvitationSendResult(
                    InvitationSendResultCode.NotSent,
                    "the demo sender is set to fail"
                )
            );

        store.Last = delivery;
        return Task.FromResult(
            new InvitationSendResult(
                InvitationSendResultCode.Sent,
                $"Invitation sent to {delivery.Email}. In this demo it is on the invitation preview page: {PreviewUrl()}"
            )
        );
    }

    private string PreviewUrl()
    {
        var addresses = server.Features.Get<IServerAddressesFeature>()?.Addresses ?? [];
        var origin =
            addresses.FirstOrDefault(a => a.StartsWith("https://"))
            ?? addresses.FirstOrDefault()
            ?? string.Empty;
        return $"{origin.TrimEnd('/')}{WebAppRoutes.InvitationPreview}";
    }
}
