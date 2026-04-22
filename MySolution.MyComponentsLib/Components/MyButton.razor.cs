using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace MySolution.MyComponentsLib.Components;

public partial class MyButton : ComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JsRuntime { get; set; } = default!;

    [Parameter] public string Text { get; set; } = "Click Me!";
    [Parameter] public string IconClass { get; set; } = "bi bi-lightning-fill";
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public bool IsLoading { get; set; }

    private ElementReference _buttonRef;
    private IJSObjectReference? _module;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/MySolution.MyComponentsLib/Components/MyButton.razor.js");
        }
    }

    private async Task HandleClick(MouseEventArgs e)
    {
        if (Disabled || IsLoading) return;

        if (_module is not null)
        {
            await _module.InvokeVoidAsync("createRipple", _buttonRef, e.ClientX, e.ClientY);
            await _module.InvokeVoidAsync("playSuccessSound");
        }

        await OnClick.InvokeAsync(e);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            await _module.DisposeAsync();
        }
    }
}
