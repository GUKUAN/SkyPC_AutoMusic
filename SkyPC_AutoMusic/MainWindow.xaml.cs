using MaterialDesignThemes.Wpf;
using Prism.Events;
using SkyPC_AutoMusic.Event;
using SkyPC_AutoMusic.View;
using SkyPC_AutoMusic.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SkyPC_AutoMusic
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        //应为焦点的窗口
        public IntPtr lastActiveWindowHandle = IntPtr.Zero;

        double originalHeight;
        public static MainWindow Instance { get; private set; }

        //全局热键
        private const int HOTKEY_PLAY_PAUSE = 0x4D01;
        private const int HOTKEY_PREVIOUS = 0x4D02;
        private const int HOTKEY_NEXT = 0x4D03;

        private IntPtr windowHandle = IntPtr.Zero;
        private HwndSource hwndSource;
        private bool hotkeysWanted;

        public MainWindow()
        {
            //事件聚合器订阅
            EA.EventAggregator.GetEvent<WindowMinimizeEvent>().Subscribe(() => { WindowState = WindowState.Minimized; });
            EA.EventAggregator.GetEvent<WindowCloseEvent>().Subscribe(Close);
            EA.EventAggregator.GetEvent<WindowExpandEvent>().Subscribe(WindowExpand);
            EA.EventAggregator.GetEvent<AddBackgroundEvent>().Subscribe(AddBackground);
            EA.EventAggregator.GetEvent<DeleteBackgroundEvent>().Subscribe(DeleteBackground);
            EA.EventAggregator.GetEvent<SendMessageSnackbar>().Subscribe(SendMessageSnackbar);
            EA.EventAggregator.GetEvent<SwitchHotkeysEvent>().Subscribe(ApplyHotkeys);
            //初始化
            InitializeComponent();
            originalHeight = this.Height;
            Instance = this;
            //防止窗口成为焦点
            lastActiveWindowHandle = Win32.GetForegroundWindow();
            Deactivated += MainWindow_Deactivated;
            //拖拽导入
            AllowDrop = true;
            DragOver += MainWindow_DragOver;
            Drop += MainWindow_Drop;
            SourceInitialized += (object sender, EventArgs e) =>
            {
                var handle = new WindowInteropHelper(this).Handle;
                var exstyle = Win32.GetWindowLong(handle, Win32.GWL_EXSTYLE);
                exstyle |= Win32.WS_EX_NOACTIVATE;
                Win32.SetWindowLong(handle, Win32.GWL_EXSTYLE, exstyle);
                //窗口句柄好了，挂上热键的消息钩子
                windowHandle = handle;
                hwndSource = HwndSource.FromHwnd(handle);
                if (hwndSource != null)
                    hwndSource.AddHook(WndProc);
                ApplyHotkeys(hotkeysWanted);
            };
            Closed += (object sender, EventArgs e) => { UnregisterHotkeys(); };
            //窗口出来后立刻建好列表（此时播放页已就绪），保证市场导入与列表恢复不依赖用户先点哪个页
            Loaded += (object sender, RoutedEventArgs e) => { ListViewModel.EnsureCreated(); };
        }

        

        //修改背景图像
        private void AddBackground(BitmapImage img)
        {
            //string imgPath = AppDomain.CurrentDomain.BaseDirectory + "bg.jpg";
            //BitmapImage img = new BitmapImage(new Uri(imgPath));
            Body.Background = Brushes.Transparent;
            BgImage.Source = img;
        }

        //删除背景图像
        private void DeleteBackground()
        {
            Body.Background = Brushes.Transparent;
            BgImage.Source = null;
        }

        //窗体位置拖动
        private void title_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        //窗口展开或收起（带高度动画）
        private void WindowExpand(bool isExpand)
        {
            double target;
            if(isExpand)
            {
                target = originalHeight;
                TitleBar.Children.Clear();
                TitleBar.Children.Add(new UserControlTitleBarNormal());
            }
            else
            {
                //收起前记住当前高度，展开时好还原（含用户手动缩放后的高度）
                originalHeight = Height;
                target = TitleBar.ActualHeight;
                TitleBar.Children.Clear();
                TitleBar.Children.Add(new UserControlTitleBarDetail());
            }

            QuadraticEase ease = new QuadraticEase { EasingMode = EasingMode.EaseInOut };
            BeginAnimation(HeightProperty, new DoubleAnimation(target, TimeSpan.FromMilliseconds(240)) { EasingFunction = ease });
        }

        //页面切换动效：内容淡入 + 轻微上移
        private void Body_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TabItem item = Body.SelectedItem as TabItem;
            if (item == null)
                return;
            UIElement content = item.Content as UIElement;
            if (content == null)
                return;

            TranslateTransform translate = content.RenderTransform as TranslateTransform;
            if (translate == null)
            {
                translate = new TranslateTransform();
                content.RenderTransform = translate;
            }

            QuadraticEase ease = new QuadraticEase { EasingMode = EasingMode.EaseOut };
            content.Opacity = 0;
            translate.Y = 12;
            content.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(200)) { EasingFunction = ease });
            translate.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(220)) { EasingFunction = ease });
        }

        //消息框
        private void SendMessageSnackbar(string message)
        {
            var messageQueue = Snackbar.MessageQueue;
            messageQueue.Enqueue(message, Properties.Resources.Options_Confirm, (param) => { CloseMessage(); }, null, false, true, TimeSpan.FromMilliseconds(3000));
        }

        private void CloseMessage()
        {
            Snackbar.IsActive = false;
        }

        private void MainWindow_Deactivated(object sender, EventArgs e)
        {
            lastActiveWindowHandle = Win32.GetForegroundWindow();
        }

        //拖拽文件进来
        private void MainWindow_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void MainWindow_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
                EA.EventAggregator.GetEvent<AddFilesEvent>().Publish(files);
        }

        //开关全局热键
        private void ApplyHotkeys(bool enable)
        {
            hotkeysWanted = enable;
            if (windowHandle == IntPtr.Zero)
                return;

            UnregisterHotkeys();
            if (!enable)
                return;

            uint modifiers = Win32.MOD_CONTROL | Win32.MOD_ALT;
            Win32.RegisterHotKey(windowHandle, HOTKEY_PLAY_PAUSE, modifiers, 0x20);//空格
            Win32.RegisterHotKey(windowHandle, HOTKEY_PREVIOUS, modifiers, 0x25);//左
            Win32.RegisterHotKey(windowHandle, HOTKEY_NEXT, modifiers, 0x27);//右
        }

        private void UnregisterHotkeys()
        {
            if (windowHandle == IntPtr.Zero)
                return;

            Win32.UnregisterHotKey(windowHandle, HOTKEY_PLAY_PAUSE);
            Win32.UnregisterHotKey(windowHandle, HOTKEY_PREVIOUS);
            Win32.UnregisterHotKey(windowHandle, HOTKEY_NEXT);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == Win32.WM_HOTKEY)
            {
                switch (wParam.ToInt32())
                {
                    case HOTKEY_PLAY_PAUSE:
                        EA.EventAggregator.GetEvent<PauseSongEvent>().Publish();
                        break;
                    case HOTKEY_PREVIOUS:
                        EA.EventAggregator.GetEvent<NextPreviousSongEvent>().Publish(false);
                        break;
                    case HOTKEY_NEXT:
                        EA.EventAggregator.GetEvent<NextPreviousSongEvent>().Publish(true);
                        break;
                }
                handled = true;
            }
            return IntPtr.Zero;
        }
    }
}
