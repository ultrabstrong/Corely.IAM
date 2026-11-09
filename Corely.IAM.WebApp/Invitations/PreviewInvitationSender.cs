using Corely.IAM.Invitations.Models;
using Corely.IAM.Invitations.Providers;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Options;

namespace Corely.IAM.WebApp.Invitations;

/// <summary>
/// Stands in for email in the reference app: the last invitation is kept in memory and shown on
/// the invitation preview page.
/// </summary>
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

    // The sender runs outside any request, so the site's address comes from the server itself.
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
