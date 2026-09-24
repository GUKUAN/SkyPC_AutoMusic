using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkyPC_AutoMusic.Event
{
    //切换全局热键开关
    internal class SwitchHotkeysEvent : PubSubEvent<bool>
    {
    }
}
