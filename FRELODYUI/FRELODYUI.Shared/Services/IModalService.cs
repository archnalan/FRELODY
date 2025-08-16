using FRELODYUI.Shared.Models;

namespace FRELODYUI.Shared.Services
{
    public interface IModalService
    {
        ModalOptionDto CurrentModal { get; }
        bool IsModalVisible { get; }

        event Action? OnClose;
        event Action? OnShow;

        void Close();
        void Show(ModalOptionDto option);
    }
}