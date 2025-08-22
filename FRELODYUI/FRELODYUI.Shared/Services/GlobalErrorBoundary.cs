using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FRELODYUI.Shared.Services
{
    public class GlobalErrorBoundary : ErrorBoundary
    {
        [Inject] private IServiceProvider Services { get; set; } = default!;
        [Inject] private ILogger<GlobalErrorBoundary> _logger { get; set; } = default!;

        protected override Task OnErrorAsync(Exception exception)
        {
            // Always log
            _logger.LogError(exception, "Unhandled exception in component subtree.");
            //Get host environment and return error in development
            return base.OnErrorAsync(exception);
        }
    }
}
