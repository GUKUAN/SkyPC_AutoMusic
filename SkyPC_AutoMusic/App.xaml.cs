using SkyPC_AutoMusic.Event;
using SkyPC_AutoMusic.Model;
using SkyPC_AutoMusic.Properties;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace SkyPC_AutoMusic
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        string text = SkyPC_AutoMusic.Properties.Resources.UnhandledException + "\n\n";

        public App()
        {
            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = (Exception)e.ExceptionObject;
            string content = text + exception.Message + "\n" + exception.StackTrace;
            MessageBox.Show(content, "意外的操作", MessageBoxButton.OK, MessageBoxImage.Error);

            Environment.Exit(1);
        }

        private void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            string content = text + e.Exception.Message + "\n" + e.Exception.StackTrace;
            MessageBox.Show(content, "意外的操作", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;

            Environment.Exit(1);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            //先把设置读进来（不依赖界面是否打开）
            SkyPC_AutoMusic.Model.Settings.Load();
            //再按语言设置界面文化
            switch (SkyPC_AutoMusic.Model.Settings.Instance.LanguageCode)
            {
                case "zh-CN":
                    //中文
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("zh-CN");
                    break;
                case "en-US":
                    //英语
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
                    break;
                default:
                    //自动
                    break;
            }
            //初始化主题系统并应用上次选择
            SkyPC_AutoMusic.Theme.Themes.SetCurrent(SkyPC_AutoMusic.Model.Settings.Instance.ThemeId);
            SkyPC_AutoMusic.Theme.ThemeService.Initialize();
        }

    }
}
