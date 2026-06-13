using FRELODYAPP.Dtos.AuthDtos;
using FRELODYUI.Shared.Services;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace FRELODYUI.Web.Services
{
    /// <summary>
    /// Captures the active Blazor circuit's <see cref="IServiceProvider"/> into an
    /// <see cref="AsyncLocal{T}"/> for the duration of each inbound circuit activity
    /// (render, event, JS interop callback). This lets code running in a *different* DI
    /// scope — notably an <c>IHttpClientFactory</c> message handler — reach back into the
    /// circuit's scoped services. Per-activity + per-circuit, so no cross-user bleed.
    /// Documented pattern: "Access server-side Blazor services from a different DI scope".
    /// </summary>
    public sealed class CircuitServicesAccessor
    {
        private static readonly AsyncLocal<IServiceProvider?> _current = new();

        public IServiceProvider? Services
        {
            get => _current.Value;
            set => _current.Value = value;
        }
    }

    /// <summary>Sets <see cref="CircuitServicesAccessor.Services"/> around every inbound activity.</summary>
    internal sealed class ServicesAccessorCircuitHandler : CircuitHandler
    {
        private readonly IServiceProvider _services;
        private readonly CircuitServicesAccessor _accessor;

        public ServicesAccessorCircuitHandler(IServiceProvider services, CircuitServicesAccessor accessor)
        {
            _services = services;
            _accessor = accessor;
        }

        public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(
            Func<CircuitInboundActivityContext, Task> next) =>
            async context =>
            {
                _accessor.Services = _services;
                try
                {
                    await next(context);
                }
                finally
                {
                    _accessor.Services = null;
                }
            };
    }

    /// <summary>
    /// Reads the session from the <i>circuit's</i> scope when the calling handler's own
    /// scope can't reach the browser. Prefers the in-memory cached session (no interop);
    /// falls back to the circuit's connected localStorage.
    /// </summary>
    internal sealed class CircuitTokenFallback : ICircuitTokenFallback
    {
        private readonly CircuitServicesAccessor _accessor;

        public CircuitTokenFallback(CircuitServicesAccessor accessor) => _accessor = accessor;

        public async Task<LoginResponseDto?> GetSessionAsync()
        {
            var circuit = _accessor.Services;
            if (circuit is null) return null; // not inside a circuit activity

            // Cheapest path: the circuit's auth provider may already hold the session.
            var auth = circuit.GetService<GlobalAuthStateProvider>();
            if (auth?.CachedSession is { } cached) return cached;

            // Otherwise read the circuit's localStorage (its IJSRuntime IS browser-connected).
            var storage = circuit.GetService<IStorageService>();
            if (storage is null) return null;
            try
            {
                return await storage.GetItemAsync<LoginResponseDto>("sessionState");
            }
            catch
            {
                return null;
            }
        }
    }

    public static class CircuitServicesAccessorExtensions
    {
        /// <summary>Registers the circuit-scope bridge used by <c>AuthHeaderHandler</c> on the server.</summary>
        public static IServiceCollection AddCircuitServicesAccessor(this IServiceCollection services)
        {
            services.AddSingleton<CircuitServicesAccessor>();
            services.AddScoped<CircuitHandler, ServicesAccessorCircuitHandler>();
            services.AddSingleton<ICircuitTokenFallback, CircuitTokenFallback>();
            return services;
        }
    }
}
