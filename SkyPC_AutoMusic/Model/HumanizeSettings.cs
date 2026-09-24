using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkyPC_AutoMusic.Model
{
    //拟人化的全部可配置参数
    public class HumanizeSettings : INotifyPropertyChanged
    {
        //任意一项改动都触发（由设置页收集后统一存盘）
        public event Action Changed;

        public HumanizeSettings()
        {
            Strength = 1.0;
            SeedMode = "PerSheet";
            JitterMode = "Uniform";
            JitterExpr = String.Empty;
            DropChance = 0.02;
            WrongChance = 0.01;
            DriftMin = -3;
            DriftMax = 3;
            BreathChance = 0.5;
            BreathMin = 80;
            BreathMax = 300;
            FatigueStart = 0.6;
            FatigueEnd = 1.6;
            GraceChance = 0.005;
            JitterMin = -15;
            JitterMax = 15;
            HoldMin = -20;
            HoldMax = 20;
            SpreadMin = 0;
            SpreadMax = 25;
        }

        //总开关与总强度
        private bool enabled;
        public bool Enabled { get { return enabled; } set { Set(ref enabled, value); } }

        private double strength;
        public double Strength { get { return strength; } set { Set(ref strength, value); } }

        //种子策略：PerSheet=每谱固定，PerPlay=每次随机
        private string seedMode;
        public string SeedMode { get { return seedMode; } set { Set(ref seedMode, value); } }

        //节拍微抖动
        private bool jitterEnabled;
        public bool JitterEnabled { get { return jitterEnabled; } set { Set(ref jitterEnabled, value); } }

        private double jitterMin;
        public double JitterMin { get { return jitterMin; } set { Set(ref jitterMin, value); } }

        private double jitterMax;
        public double JitterMax { get { return jitterMax; } set { Set(ref jitterMax, value); } }

        private string jitterMode;
        public string JitterMode { get { return jitterMode; } set { Set(ref jitterMode, value); } }

        private string jitterExpr;
        public string JitterExpr { get { return jitterExpr; } set { Set(ref jitterExpr, value); } }

        //按键时长随机
        private bool holdEnabled;
        public bool HoldEnabled { get { return holdEnabled; } set { Set(ref holdEnabled, value); } }

        private double holdMin;
        public double HoldMin { get { return holdMin; } set { Set(ref holdMin, value); } }

        private double holdMax;
        public double HoldMax { get { return holdMax; } set { Set(ref holdMax, value); } }

        //随机漏音
        private bool dropEnabled;
        public bool DropEnabled { get { return dropEnabled; } set { Set(ref dropEnabled, value); } }

        private double dropChance;
        public double DropChance { get { return dropChance; } set { Set(ref dropChance, value); } }

        //随机错音
        private bool wrongEnabled;
        public bool WrongEnabled { get { return wrongEnabled; } set { Set(ref wrongEnabled, value); } }

        private double wrongChance;
        public double WrongChance { get { return wrongChance; } set { Set(ref wrongChance, value); } }

        //和弦错峰
        private bool spreadEnabled;
        public bool SpreadEnabled { get { return spreadEnabled; } set { Set(ref spreadEnabled, value); } }

        private double spreadMin;
        public double SpreadMin { get { return spreadMin; } set { Set(ref spreadMin, value); } }

        private double spreadMax;
        public double SpreadMax { get { return spreadMax; } set { Set(ref spreadMax, value); } }

        //速度轻微浮动（百分比）
        private bool driftEnabled;
        public bool DriftEnabled { get { return driftEnabled; } set { Set(ref driftEnabled, value); } }

        private double driftMin;
        public double DriftMin { get { return driftMin; } set { Set(ref driftMin, value); } }

        private double driftMax;
        public double DriftMax { get { return driftMax; } set { Set(ref driftMax, value); } }

        //乐句换气停顿
        private bool breathEnabled;
        public bool BreathEnabled { get { return breathEnabled; } set { Set(ref breathEnabled, value); } }

        private double breathChance;
        public double BreathChance { get { return breathChance; } set { Set(ref breathChance, value); } }

        private double breathMin;
        public double BreathMin { get { return breathMin; } set { Set(ref breathMin, value); } }

        private double breathMax;
        public double BreathMax { get { return breathMax; } set { Set(ref breathMax, value); } }

        //疲劳感（从开头到结尾的误差倍率）
        private bool fatigueEnabled;
        public bool FatigueEnabled { get { return fatigueEnabled; } set { Set(ref fatigueEnabled, value); } }

        private double fatigueStart;
        public double FatigueStart { get { return fatigueStart; } set { Set(ref fatigueStart, value); } }

        private double fatigueEnd;
        public double FatigueEnd { get { return fatigueEnd; } set { Set(ref fatigueEnd, value); } }

        //随机加花/装饰音
        private bool graceEnabled;
        public bool GraceEnabled { get { return graceEnabled; } set { Set(ref graceEnabled, value); } }

        private double graceChance;
        public double GraceChance { get { return graceChance; } set { Set(ref graceChance, value); } }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Set<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;
            field = value;
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            if (Changed != null)
                Changed();
        }

        //把读进来的设置逐项拷进来（保持对象引用不变）
        public void CopyFrom(HumanizeSettings source)
        {
            if (source == null)
                return;
            Enabled = source.Enabled;
            Strength = source.Strength;
            SeedMode = source.SeedMode;
            JitterEnabled = source.JitterEnabled;
            JitterMin = source.JitterMin;
            JitterMax = source.JitterMax;
            JitterMode = source.JitterMode;
            JitterExpr = source.JitterExpr;
            HoldEnabled = source.HoldEnabled;
            HoldMin = source.HoldMin;
            HoldMax = source.HoldMax;
            DropEnabled = source.DropEnabled;
            DropChance = source.DropChance;
            WrongEnabled = source.WrongEnabled;
            WrongChance = source.WrongChance;
            SpreadEnabled = source.SpreadEnabled;
            SpreadMin = source.SpreadMin;
            SpreadMax = source.SpreadMax;
            DriftEnabled = source.DriftEnabled;
            DriftMin = source.DriftMin;
            DriftMax = source.DriftMax;
            BreathEnabled = source.BreathEnabled;
            BreathChance = source.BreathChance;
            BreathMin = source.BreathMin;
            BreathMax = source.BreathMax;
            FatigueEnabled = source.FatigueEnabled;
            FatigueStart = source.FatigueStart;
            FatigueEnd = source.FatigueEnd;
            GraceEnabled = source.GraceEnabled;
            GraceChance = source.GraceChance;
        }
    }
}
