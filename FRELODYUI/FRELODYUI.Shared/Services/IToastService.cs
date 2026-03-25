using FRELODYUI.Shared.Models;

namespace FRELODYUI.Shared.Services
{
    public interface IToastService
    {
        string Message { get; }
        ToastType Type { get; }
        bool IsVisible { get; }
        event Action? OnToast;
        void Show(string message, ToastType type = ToastType.Success, int durationMs = 3000);
        void Hide();
    }
}
