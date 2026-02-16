using Microsoft.AspNetCore.Components;

namespace Corely.IAM.Web.Components;

public abstract class EntityDetailPageBase : EntityPageBase
{
    [Parameter]
    public Guid Id { get; set; }
}
