using MudBlazor;
using MudBlazor.Services;

namespace Web.Frontend.ComponentTests.Common;

public abstract class MudBunitContext : BunitContext
{
    protected MudBunitContext()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    protected void RenderMudPopoverProvider() => Render<MudPopoverProvider>();
}
