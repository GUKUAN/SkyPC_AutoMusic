using MaterialDesignThemes.Wpf;
using SkyPC_AutoMusic.Command;
using SkyPC_AutoMusic.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SkyPC_AutoMusic.ViewModel
{
    //键位对话框里的单格
    internal class KeybindSlot : NotificationObject
    {
        public int Index { get; set; }

        public string NoteLabel { get; set; }

        private string keyName;

        public string KeyName
        {
            get { return keyName; }
            set { keyName = value; OnPropertyChanged(); }
        }
    }

    internal class KeybindDialogViewModel : NotificationObject
    {
        private int activeIndex;

        private List<int> workingKeys;

        public ObservableCollection<KeybindSlot> Slots { get; private set; }

        public int ActiveIndex
        {
            get { return activeIndex; }
            set { activeIndex = value; OnPropertyChanged(); }
        }

        public DelegateCommand ResetCommand { get; set; }

        public DelegateCommand ConfirmCommand { get; set; }

        public KeybindDialogViewModel()
        {
            workingKeys = new List<int>(Settings.Instance.CustomKeys);
            Slots = new ObservableCollection<KeybindSlot>();
            for (int i = 0; i < workingKeys.Count; i++)
            {
                Slots.Add(new KeybindSlot
                {
                    Index = i,
                    NoteLabel = (i + 1).ToString(),
                    KeyName = KeyText(workingKeys[i])
                });
            }
            activeIndex = 0;
            ResetCommand = new DelegateCommand(ResetDefault);
            ConfirmCommand = new DelegateCommand(Confirm);
        }

        //把某个音符绑到新的键值
        public void SetKey(int index, int virtualKey)
        {
            if (index < 0 || index >= workingKeys.Count)
                return;
            workingKeys[index] = virtualKey;
            Slots[index].KeyName = KeyText(virtualKey);
            ActiveIndex = index;
        }

        private void ResetDefault()
        {
            workingKeys = Settings.DefaultCustomKeys();
            for (int i = 0; i < Slots.Count; i++)
            {
                Slots[i].KeyName = KeyText(workingKeys[i]);
            }
        }

        private void Confirm()
        {
            Settings.Instance.CustomKeys = new List<int>(workingKeys);
            Settings.Save();
            DialogHost.CloseDialogCommand.Execute(null, null);
        }

        //把虚拟键值翻译成好认的按键名
        public static string KeyText(int virtualKey)
        {
            Key key = KeyInterop.KeyFromVirtualKey(virtualKey);
            switch (key)
            {
                case Key.OemSemicolon: return ";";
                case Key.OemComma: return ",";
                case Key.OemPeriod: return ".";
                case Key.OemQuestion: return "/";
                case Key.OemPlus: return "=";
                case Key.OemMinus: return "-";
                case Key.OemOpenBrackets: return "[";
                case Key.OemCloseBrackets: return "]";
                case Key.OemPipe: return "\\";
                case Key.OemQuotes: return "'";
                case Key.OemTilde: return "`";
                case Key.Space: return "Space";
                case Key.None: return "?";
                default:
                    if (key >= Key.D0 && key <= Key.D9)
                        return ((int)key - (int)Key.D0).ToString();
                    return key.ToString();
            }
        }
    }
}
