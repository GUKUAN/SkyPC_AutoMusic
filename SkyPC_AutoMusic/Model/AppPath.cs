using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkyPC_AutoMusic.Model
{
    //程序相关的文件路径统一放这里，避免到处硬编码
    static class AppPath
    {
        //配置文件（固定放在程序目录，防止从别的“起始位置”启动时读写到别处）
        public static string Settings
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.xml"); }
        }

        //自定义背景图候选（先 jpg 后 png）
        public static IEnumerable<string> BackgroundImages
        {
            get
            {
                yield return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bg.jpg");
                yield return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bg.png");
            }
        }
    }
}
