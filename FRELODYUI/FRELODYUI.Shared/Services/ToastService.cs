using FRELODYUI.Shared.Models;

namespace FRELODYUI.Shared.Services
{
    public class ToastService : IToastService, IDisposable
    {
        public string Message { get; private set; } = string.Empty;
        public ToastType Type { get; private set; } = ToastType.Success;
        public bool IsVisible { get; private set; }
        public event Action? OnToast;

        private System.Threading.Timer? _timer;

        public void Show(string message, ToastType type = ToastType.Success, int durationMs = 3000)
        {
            _timer?.Dispose();
            Message = message;
            Type = type;
            IsVisible = true;
            OnToast?.Invoke();
            _timer = new System.Threading.Timer(_ => Hide(), null, durationMs, System.Threading.Timeout.Infinite);
        }

        public void Hide()
        {
            IsVisible = false;
            OnToast?.Invoke();
        }

        public void Dispose() => _timer?.Dispose();
    }
}
