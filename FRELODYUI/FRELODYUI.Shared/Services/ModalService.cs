using FRELODYUI.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYUI.Shared.Services
{
    public class ModalService : IModalService
    {
        public bool IsModalVisible { get; private set; }
        public ModalOptionDto CurrentModal { get; private set; } = new();

        public event Action? OnShow;
        public event Action? OnClose;

        public void Show(ModalOptionDto option)
        {
            CurrentModal = option;
            IsModalVisible = true;
            OnShow?.Invoke();
        }

        public void Close()
        {
            IsModalVisible = false;
            CurrentModal = new();
            OnClose?.Invoke();
        }
    }
}
