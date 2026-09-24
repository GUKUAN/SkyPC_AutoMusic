using System;
using System.Collections.Generic;

namespace SkyPC_AutoMusic.Model
{
    //按当前设置给每一次演奏生成拟人化误差
    //同一个种子 + 同一个拍号，结果稳定可复现
    public class Humanizer
    {
        private readonly HumanizeSettings settings;
        private readonly int seed;
        private readonly int beatCount;
        private readonly double driftStart;
        private readonly double driftEnd;

        public Humanizer(HumanizeSettings settings, int seed, int beatCount)
        {
            this.settings = settings;
            this.seed = seed;
            this.beatCount = Math.Max(1, beatCount);

            Random pick = new Random(unchecked(seed ^ 0x5F3759DF));
            driftStart = Lerp(settings.DriftMin, settings.DriftMax, pick.NextDouble());
            driftEnd = Lerp(settings.DriftMin, settings.DriftMax, pick.NextDouble());
        }

        //每拍的触发时间偏移（毫秒）
        public double TriggerOffset(int beat, int timeMs)
        {
            double fatigue = Fatigue(beat);
            double jitter = settings.JitterEnabled ? SampleJitter(beat) * fatigue : 0;
            double drift = settings.DriftEnabled ? DriftOffset(beat, timeMs) : 0;
            return (jitter + drift) * settings.Strength;
        }

        //对当前拍音符做漏音/错音处理
        public List<NoteKey> ProcessKeys(List<NoteKey> keys, int beat)
        {
            if (!settings.DropEnabled && !settings.WrongEnabled)
                return keys;

            double factor = Fatigue(beat) * settings.Strength;
            Random rnd = Rnd(beat, 2);
            List<NoteKey> result = new List<NoteKey>(keys.Count);
            foreach (NoteKey key in keys)
            {
                if (settings.DropEnabled && rnd.NextDouble() < settings.DropChance * factor)
                    continue;
                NoteKey k = key;
                if (settings.WrongEnabled && rnd.NextDouble() < settings.WrongChance * factor)
                    k = Neighbor(k, rnd);
                result.Add(k);
            }
            return result;
        }

        //按键时长（毫秒）
        public int Hold(int beat, int baseMs)
        {
            if (!settings.HoldEnabled)
                return baseMs;

            double factor = Fatigue(beat) * settings.Strength;
            double min = Math.Min(settings.HoldMin, settings.HoldMax);
            double max = Math.Max(settings.HoldMin, settings.HoldMax);
            double delta = min + (max - min) * Rnd(beat, 3).NextDouble();
            return Math.Max(5, (int)Math.Round(baseMs + delta * factor));
        }

        //和弦各音之间的错峰间隔（毫秒），无启用则返回 null
        public int[] Spread(int keyCount, int beat)
        {
            if (!settings.SpreadEnabled || keyCount <= 1)
                return null;

            double min = Math.Min(settings.SpreadMin, settings.SpreadMax);
            double max = Math.Max(settings.SpreadMin, settings.SpreadMax);
            Random rnd = Rnd(beat, 4);
            int[] delays = new int[keyCount];
            for (int i = 0; i < keyCount; i++)
                delays[i] = Math.Max(0, (int)Math.Round((min + (max - min) * rnd.NextDouble()) * settings.Strength));
            return delays;
        }

        //空拍时可能插入的换气停顿（毫秒）
        public double Breath(int beat)
        {
            if (!settings.BreathEnabled)
                return 0;

            Random rnd = Rnd(beat, 5);
            if (rnd.NextDouble() > settings.BreathChance * settings.Strength)
                return 0;
            double min = Math.Min(settings.BreathMin, settings.BreathMax);
            double max = Math.Max(settings.BreathMin, settings.BreathMax);
            return (min + (max - min) * rnd.NextDouble()) * settings.Strength;
        }

        //极低概率插入一个装饰音
        public bool TryGrace(int beat, out NoteKey key)
        {
            key = NoteKey._1Key0;
            if (!settings.GraceEnabled)
                return false;
            Random rnd = Rnd(beat, 6);
            if (rnd.NextDouble() >= settings.GraceChance * settings.Strength)
                return false;
            key = (NoteKey)rnd.Next(0, 15);
            return true;
        }

        //从开头到结尾的疲劳倍率
        private double Fatigue(int beat)
        {
            if (!settings.FatigueEnabled)
                return 1.0;
            return Lerp(settings.FatigueStart, settings.FatigueEnd, beat / (double)beatCount);
        }

        private double DriftOffset(int beat, int timeMs)
        {
            double percent = Lerp(driftStart, driftEnd, beat / (double)beatCount);
            return percent / 100.0 * timeMs;
        }

        private double SampleJitter(int beat)
        {
            double min = Math.Min(settings.JitterMin, settings.JitterMax);
            double max = Math.Max(settings.JitterMin, settings.JitterMax);
            double range = max - min;
            Random rnd = Rnd(beat, 1);
            double r = rnd.NextDouble();

            switch (settings.JitterMode)
            {
                case "Gaussian":
                    double mean = (min + max) / 2.0;
                    double sigma = range / 6.0;
                    double g = mean + sigma * Gaussian(rnd);
                    return Math.Min(max, Math.Max(min, g));
                case "Triangular":
                    return min + range * (r + rnd.NextDouble()) / 2.0;
                case "Early":
                    return min + range * r * r;
                case "Late":
                    return max - range * r * r;
                case "Custom":
                    return SampleCustom(beat, min, max, r);
                default:
                    return min + range * r;
            }
        }

        private double SampleCustom(int beat, double min, double max, double r)
        {
            Dictionary<string, double> vars = new Dictionary<string, double>
            {
                { "r", r },
                { "min", min },
                { "max", max },
                { "beat", beat },
                { "count", beatCount },
                { "fatigue", Fatigue(beat) },
                { "strength", settings.Strength }
            };
            double value;
            if (HumanizeExpression.TryEval(settings.JitterExpr, vars, out value))
                return value;
            //表达式写错就退回均匀分布
            return min + (max - min) * r;
        }

        private static NoteKey Neighbor(NoteKey key, Random rnd)
        {
            int index = (int)key % 15;
            int step = rnd.Next(0, 2) == 0 ? -1 : 1;
            int next = index + step;
            if (next < 0) next = 1;
            if (next > 14) next = 13;
            return (NoteKey)next;
        }

        private static double Gaussian(Random rnd)
        {
            double u1 = 1.0 - rnd.NextDouble();
            double u2 = 1.0 - rnd.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        }

        private static double Lerp(double a, double b, double t)
        {
            return a + (b - a) * t;
        }

        //同种子、同拍号、同盐值 -> 同一个随机序列
        private Random Rnd(int beat, int salt)
        {
            unchecked
            {
                return new Random(seed + beat * 486187739 + salt * 16777619);
            }
        }
    }
}
