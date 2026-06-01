using Microsoft.JSInterop;

namespace FRELODY.Docs.Services;

/// <summary>
/// Light/dark theme toggle. Mirrors the main FRELODY app: the active theme is
/// driven by the <c>data-bs-theme</c> attribute on the document element, with
/// the choice persisted to localStorage by the <c>frelodyTheme</c> JS helper.
/// </summary>
public class ThemeService
{
    private readonly IJSRuntime _js;
    public event Action? ThemeChanged;

    public string Current { get; private set; } = "light";

    public ThemeService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task InitAsync()
    {
        try
        {
            Current = await _js.InvokeAsync<string>("frelodyTheme.get") ?? "light";
        }
        catch
        {
            Current = "light";
        }
    }

    public async Task ToggleAsync()
    {
        Current = Current == "dark" ? "light" : "dark";
        await _js.InvokeVoidAsync("frelodyTheme.set", Current);
        ThemeChanged?.Invoke();
    }
}
