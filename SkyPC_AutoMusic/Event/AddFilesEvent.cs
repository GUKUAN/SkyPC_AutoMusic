using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkyPC_AutoMusic.Event
{
    //拖拽文件进窗口时触发，携带文件路径
    internal class AddFilesEvent : PubSubEvent<string[]>
    {
    }
}
