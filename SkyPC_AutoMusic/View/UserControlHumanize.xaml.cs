using SkyPC_AutoMusic.Model;
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
using System.Windows.Threading;

namespace SkyPC_AutoMusic.View
{
    /// <summary>
    /// UserControlHumanize.xaml 的交互逻辑
    /// </summary>
    public partial class UserControlHumanize : UserControl
    {
        private DispatcherTimer saveTimer;

        public UserControlHumanize()
        {
            InitializeComponent();
            HumanizeSettings humanize = Settings.Instance.Humanize;
            DataContext = humanize;
            humanize.Changed += ScheduleSave;
        }

        //改动后延迟存盘，避免拖动滑条时频繁写文件
        private void ScheduleSave()
        {
            if (saveTimer == null)
            {
                saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
                saveTimer.Tick += (sender, e) => { saveTimer.Stop(); Settings.Save(); };
            }
            saveTimer.Stop();
            saveTimer.Start();
        }
    }
}
