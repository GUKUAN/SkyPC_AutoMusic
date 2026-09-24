using SkyPC_AutoMusic.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SkyPC_AutoMusic.View
{
    /// <summary>
    /// UserControlKeybindDialog.xaml 的交互逻辑
    /// </summary>
    public partial class UserControlKeybindDialog : UserControl
    {
        public UserControlKeybindDialog()
        {
            InitializeComponent();
            DataContext = new KeybindDialogViewModel();
            Focusable = true;
            Loaded += (sender, e) => Keyboard.Focus(this);
        }

        //点某一格就把它设为待绑定
        private void Slot_Click(object sender, RoutedEventArgs e)
        {
            KeybindDialogViewModel model = DataContext as KeybindDialogViewModel;
            KeybindSlot slot = (sender as Button)?.DataContext as KeybindSlot;
            if (model == null || slot == null)
                return;

            model.ActiveIndex = slot.Index;
            Keyboard.Focus(this);
        }

        //按键即绑定
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            KeybindDialogViewModel model = DataContext as KeybindDialogViewModel;
            if (model == null)
            {
                base.OnPreviewKeyDown(e);
                return;
            }

            Key key = e.Key == Key.System ? e.SystemKey : e.Key;
            //修饰键、切换用的键不参与绑定
            if (IsModifierKey(key) || key == Key.Tab || key == Key.Enter || key == Key.Escape)
                return;

            int virtualKey = KeyInterop.VirtualKeyFromKey(key);
            if (virtualKey == 0)
                return;

            model.SetKey(model.ActiveIndex, virtualKey);
            e.Handled = true;
        }

        private static bool IsModifierKey(Key key)
        {
            return key == Key.LeftCtrl || key == Key.RightCtrl
                || key == Key.LeftAlt || key == Key.RightAlt
                || key == Key.LeftShift || key == Key.RightShift
                || key == Key.LWin || key == Key.RWin;
        }
    }
}
