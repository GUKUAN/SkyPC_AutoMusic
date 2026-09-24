using Newtonsoft.Json.Linq;
using SkyPC_AutoMusic.Event;
using SkyPC_AutoMusic.Event.Options;
using SkyPC_AutoMusic.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace SkyPC_AutoMusic.Model
{
    public class Player
    {

        //进度条值
        public double SliderProgress
        {
            get
            {
                if (currentSong == null || currentSong.Beats == null || currentSong.Beats.Count == 0)//没有乐谱
                    return 0;

                //播放完停在末尾，别弹回开头
                if (isPlayEnd)
                    return 1;

                //double percentage = (DateTime.Now.Subtract(startPlay).TotalMilliseconds / speedModifier) / totalTimeProgress;
                int index = currentBeatIndex;
                if (index >= currentSong.Beats.Count)
                    index = currentSong.Beats.Count - 1;
                if (index < 0)
                    index = 0;

                double percentage = (double)currentSong.Beats[index].Time / (double)totalTimeProgress;
                return percentage;
            }
            set
            {
                if (currentSong == null || currentSong.Beats == null || currentSong.Beats.Count == 0)//没有乐谱
                    return;

                //double past = totalTimeProgress * value;

                //int minDiff = int.MaxValue;
                //int noteIndex = 0;
                //for (int i = 0; i < currentSong.songNotes.Count - 1; i++)
                //{
                //    double diff = Math.Abs(currentSong.songNotes[i].time - past);
                //    if (diff < minDiff)
                //    {
                //        minDiff = (int)diff;
                //        noteIndex = i;
                //    }
                //}
                //currentNoteIndex = noteIndex;

                //startPlay = DateTime.Now.Subtract(TimeSpan.FromMilliseconds(past / speedModifier));

                //拖动进度后允许重新播放
                int index = (int)((currentSong.Beats.Count - 1) * value);
                if (index < 0)
                    index = 0;
                if (index > currentSong.Beats.Count - 1)
                    index = currentSong.Beats.Count - 1;

                currentBeatIndex = index;
                isPlayEnd = false;
                startPlay = DateTime.Now.AddMilliseconds(-currentSong.Beats[currentBeatIndex].Time / speedModifier);

                UpdateCurrentTimeLabel();

                if (isDelayToReleaseKey && !isPlayEnd && !isStop)
                {
                    //抬起所有按键
                    foreach (NoteKey notekey in Enum.GetValues(typeof(NoteKey)))
                    {
                        SendKey(notekey, false);
                    }
                }
            }
        }

        //播放时间标签
        public string CurrentTime
        {
            get
            {
                //无乐谱或已播放完毕
                if (currentSong == null || isPlayEnd)
                    return currentTime;//直接返回缓存值
                //若处于暂停状态则不更新返回值
                if (!isStop)
                {
                    UpdateCurrentTimeLabel();
                }
                return currentTime;
            }
        }

        //乐谱总时长
        public string TotalTime
        {
            get
            {
                if (currentSong == null)//没有乐谱
                    return "00:00";

                TimeSpan ts = TimeSpan.FromMilliseconds(totalTimeProgress);
                return string.Format("{0:mm\\:ss}", ts);
            }
        }
        //倍数调节滑动条
        public double SliderSpeedModifier
        {
            get
            {
                return (speedModifier * 10f);
            }
            set
            {
                //写入倍速
                speedModifier = value * 0.1f;
                //判断播放状态
                if (currentSong != null && !isStop && !isPlayEnd)
                {
                    //播放时将倍速立刻应用
                    startPlay = DateTime.Now.AddMilliseconds(-currentSong.Beats[currentBeatIndex].Time / speedModifier);
                }
            }
        }

        //当前乐谱音乐
        public Song currentSong;
        //当前乐谱索引
        public int currentSheetIndex = 0;


        //当前时长
        private string currentTime;

        // 当前节拍索引
        private int currentBeatIndex;
        // 开始播放时间
        private DateTime startPlay;
        // 节拍进度
        private int beatIntervalProgress;
        // 按下了停止
        public volatile bool isStop = true;
        // 播放完当前乐谱
        public volatile bool isPlayEnd;

        // 乐谱总时长
        private int totalTimeProgress = 0;
        // 按键持续时间
        private int durationTime = 50;
        // 倍数
        private double speedModifier = 1f;

        //键位
        private bool isUseSkyStudioKeyMapper;
        //延音
        private bool isDelayToReleaseKey;
        //后台播放
        private bool isPlayBackground;
        private IntPtr hWnd;
        //拟人化
        private Humanizer humanizer;

        // 播放完的行为
        Action playEndAction;

        #region 公开方法

        public Player(Action playEndAction)
        {
            currentTime = "00:00";
            this.playEndAction = playEndAction;
            EA.EventAggregator.GetEvent<SwitchSkyStudioKeyMapperEvent>().Subscribe((flag) => 
            { 
                isUseSkyStudioKeyMapper = flag; 
            });
            EA.EventAggregator.GetEvent<SwitchDelayToReleaseEvent>().Subscribe((flag) =>
            {
                isDelayToReleaseKey = flag;
            });
            EA.EventAggregator.GetEvent<SwitchPlayBackgroundEvent>().Subscribe((flag) => 
            { 
                isPlayBackground = flag;
                hWnd = GetGamehWnd();
            } );
        }

        public void InitializePlay()
        {
            currentTime = "00:00";
            currentBeatIndex = 0;
            isPlayEnd = false;
            startPlay = DateTime.Now;
            humanizer = null;//重新开始，按种子重新生成
            totalTimeProgress = currentSong.Beats[currentSong.Beats.Count - 1].Time;
        }

        /// <summary>
        /// 切换播放和暂停状态
        /// </summary>
        /// <returns>是否执行成功</returns>
        public bool TogglePlay()
        {
            if (currentSong == null)//没有谱子
                return false;

            if (isPlayBackground)//后台播放获取窗口句柄
            {
                hWnd = GetGamehWnd();
                if(hWnd == IntPtr.Zero)
                {
                    EA.EventAggregator.GetEvent<SendMessageSnackbar>().Publish(Properties.Resources.Play_CantFindWindow);
                    return true;
                }
            }

            if (isStop && !isPlayEnd)//暂停状态
            {
                //继续播放
                ChangeFocus();
                isStop = false;
                startPlay = DateTime.Now.AddMilliseconds(-currentSong.Beats[currentBeatIndex].Time / speedModifier);
                StartCycle();
            }
            else if (isPlayEnd)//播放完毕
            {
                //重新播放
                ChangeFocus();
                InitializePlay();
                isStop = false;
                StartCycle();
            }
            else if (!isPlayEnd)//播放状态
            {
                //暂停播放
                isStop = true;
            }

            return true;
        }

        #endregion

        #region 私有方法
        //切换焦点
        private void ChangeFocus()
        {
            if (Win32.GetForegroundWindow() == new WindowInteropHelper(MainWindow.Instance).Handle)//此软件为焦点
            {
                //切换焦点
                IntPtr hWnd = MainWindow.Instance.lastActiveWindowHandle;
                Win32.SetForegroundWindow(hWnd);
            }
        }

        private IntPtr GetGamehWnd()
        {
            IntPtr hwnd = IntPtr.Zero;

            if (hwnd == IntPtr.Zero)
                hwnd = Win32.FindWindow(null, "Sky");

            if (hwnd == IntPtr.Zero)
                hwnd = Win32.FindWindow(null, "光·遇");

            return hwnd;
        }

        private void UpdateCurrentTimeLabel()
        {
            //TimeSpan ts = DateTime.Now.Subtract(startPlay);
            //ts = TimeSpan.FromMilliseconds(ts.TotalMilliseconds * speedModifier);
            if (currentSong == null || currentSong.Beats == null || currentSong.Beats.Count == 0)
                return;

            int index = currentBeatIndex;
            if (index >= currentSong.Beats.Count)
                index = currentSong.Beats.Count - 1;
            if (index < 0)
                index = 0;

            TimeSpan ts = TimeSpan.FromMilliseconds(currentSong.Beats[index].Time);
            currentTime = string.Format("{0:mm\\:ss}", ts);
        }

        private void StartCycle()
        {
            Task.Run(() =>
            {
                //提高定时器精度，保证按键节奏
                Win32.timeBeginPeriod(1);
                try
                {
                    while (true)
                    {
                        //乐谱被清掉了就收手，别空引用
                        if (currentSong == null || currentSong.Beats == null)
                        {
                            isStop = true;
                            break;
                        }

                        //播放结束状态
                        isPlayEnd = currentBeatIndex >= currentSong.Beats.Count;

                        if (isPlayEnd)
                        {
                            //播放完毕
                            isStop = true;
                            ReleaseAllSoundingKeys();
                            playEndAction();
                            break;
                        }

                        if (isStop)
                        {
                            //手动暂停：把所有键都松开，避免卡键
                            ReleaseAllSoundingKeys();
                            break;
                        }

                        //正常播放；没到时间就小睡，别空转烧 CPU
                        if (!AutoPlay())
                            Thread.Sleep(1);
                    }
                }
                finally
                {
                    Win32.timeEndPeriod(1);
                }
            });
        }

        //松开所有正在发声的键（拟人化可能改过音，直接全抬最稳）
        private void ReleaseAllSoundingKeys()
        {
            for (int i = 0; i < 15; i++)
            {
                SendKey((NoteKey)i, false);
            }
        }

        //按设置准备拟人化处理器
        private void EnsureHumanizer()
        {
            HumanizeSettings humanize = Settings.Instance.Humanize;
            if (!humanize.Enabled)
            {
                humanizer = null;
                return;
            }
            if (humanizer != null)
                return;

            int seedBase;
            if (humanize.SeedMode == "PerPlay")
                seedBase = Environment.TickCount ^ Guid.NewGuid().GetHashCode();
            else if (currentSong.sourcePath != null)
                seedBase = currentSong.sourcePath.GetHashCode();
            else
                seedBase = currentSong.name != null ? currentSong.name.GetHashCode() : 0;

            humanizer = new Humanizer(humanize, seedBase, currentSong.Beats.Count);
        }

        //返回是否已经处理了当前节拍
        private bool AutoPlay()
        {
            //缓存
            int beatIndex = currentBeatIndex;
            if (currentSong == null || currentSong.Beats == null || beatIndex >= currentSong.Beats.Count)
                return true;

            //读取按键与时间
            List<NoteKey> sourceKeys = currentSong.Beats[beatIndex].Keys;
            int time = currentSong.Beats[beatIndex].Time;

            //判断时间（叠加拟人化的抖动/漂移）
            EnsureHumanizer();
            double offset = humanizer != null ? humanizer.TriggerOffset(beatIndex, time) : 0;
            bool flag = DateTime.Now > startPlay.AddMilliseconds(time / speedModifier + offset);

            if (!flag)
                return false;

            //空拍：可能插入换气停顿，并把时间轴往后推
            if (sourceKeys.Count == 0)
            {
                if (humanizer != null)
                {
                    double pause = humanizer.Breath(beatIndex);
                    if (pause > 0)
                        startPlay = startPlay.AddMilliseconds(pause);
                }
                currentBeatIndex++;
                return true;
            }

            //漏音/错音处理
            List<NoteKey> keys = humanizer != null ? humanizer.ProcessKeys(sourceKeys, beatIndex) : sourceKeys;
            if (keys.Count == 0)
            {
                currentBeatIndex++;
                return true;
            }

            int hold = humanizer != null ? humanizer.Hold(beatIndex, durationTime) : durationTime;
            int[] spread = humanizer != null ? humanizer.Spread(keys.Count, beatIndex) : null;

            //按下（多音带错峰）
            int staggered = 0;
            for (int i = 0; i < keys.Count; i++)
            {
                SendKey(keys[i], true);
                if (spread != null && i < keys.Count - 1)
                {
                    int gap = spread[i];
                    if (gap > 0)
                    {
                        Thread.Sleep(gap);
                        staggered += gap;
                    }
                }
            }

            //偶尔加花
            NoteKey graceKey;
            if (humanizer != null && humanizer.TryGrace(beatIndex, out graceKey))
            {
                SendKey(graceKey, true);
                Thread.Sleep(20);
                SendKey(graceKey, false);
            }

            //保持
            int remain = hold - staggered;
            if (remain > 0)
                Thread.Sleep(remain);

            //抬起
            foreach (NoteKey key in keys)
            {
                if (beatIndex == currentSong.Beats.Count - 1 || !isDelayToReleaseKey)//最后一拍或关闭延音功能
                {
                    //正常抬起
                    SendKey(key, false);
                    continue;
                }
                else if (!currentSong.Beats[beatIndex + 1].Keys.Contains(key))//下一拍不包含音节
                {
                    //延音抬起
                    SendKey(key, false);
                }//下一拍包含音节则不抬起
            }

            //更新索引
            currentBeatIndex++;
            return true;
        }

        private void SendKey(NoteKey key, bool isPress)
        {
            int noteIndex = (int)key % 15;
            byte bVk = (byte)ResolveVirtualKey(noteIndex);
            if (bVk == 0)
                return;

            byte bScan = Win32.MapVirtualKey(bVk, 0);

            if (isPress)
            {
                //按下
                if (isPlayBackground)
                {
                    Win32.PostMessage(hWnd, Win32.WM_ACTIVATE, (IntPtr)Win32.WA_ACTIVE, IntPtr.Zero);
                    int lp = 1;
                    lp |= bScan << 16;
                    Win32.PostMessage(hWnd, Win32.WM_KEYDOWN, (IntPtr)bVk, (IntPtr)lp);
                }
                else
                {
                    SendKeyInput(bVk, bScan, true);
                }
            }
            else
            {
                //释放
                if (isPlayBackground)
                {
                    Win32.PostMessage(hWnd, Win32.WM_ACTIVATE, (IntPtr)Win32.WA_ACTIVE, IntPtr.Zero);
                    int lp = 1;
                    lp |= bScan << 16;
                    lp |= 3 << 30;
                    Win32.PostMessage(hWnd, Win32.WM_KEYUP, (IntPtr)bVk, (IntPtr)lp);
                }
                else
                {
                    SendKeyInput(bVk, bScan, false);
                }
            }
        }

        //解析音符对应的虚拟键值，优先自定义键位
        private int ResolveVirtualKey(int noteIndex)
        {
            if (Settings.Instance.UseCustomKeyMapper)
            {
                List<int> keys = Settings.Instance.CustomKeys;
                if (keys != null && noteIndex >= 0 && noteIndex < keys.Count && keys[noteIndex] > 0)
                    return keys[noteIndex];
            }

            return GetPresetVirtualKey(noteIndex);
        }

        //内置的两套预设键位
        private int GetPresetVirtualKey(int noteIndex)
        {
            if (!isUseSkyStudioKeyMapper)
            {
                switch (noteIndex)//YUIOP
                {
                    case 0: return 0x59;
                    case 1: return 0x55;
                    case 2: return 0x49;
                    case 3: return 0x4F;
                    case 4: return 0x50;
                    case 5: return 0x48;
                    case 6: return 0x4A;
                    case 7: return 0x4B;
                    case 8: return 0x4C;
                    case 9: return 0xBA;
                    case 10: return 0x4E;
                    case 11: return 0x4D;
                    case 12: return 0xBC;
                    case 13: return 0xBE;
                    case 14: return 0xBF;
                    default: return 0;
                }
            }
            else
            {
                switch (noteIndex)//QWERT
                {
                    case 0: return 0x51;
                    case 1: return 0x57;
                    case 2: return 0x45;
                    case 3: return 0x52;
                    case 4: return 0x54;
                    case 5: return 0x41;
                    case 6: return 0x53;
                    case 7: return 0x44;
                    case 8: return 0x46;
                    case 9: return 0x47;
                    case 10: return 0x5A;
                    case 11: return 0x58;
                    case 12: return 0x43;
                    case 13: return 0x56;
                    case 14: return 0x42;
                    default: return 0;
                }
            }
        }

        //用 SendInput 发送扫描码，比 keybd_event 兼容性好
        private void SendKeyInput(byte bVk, byte bScan, bool isPress)
        {
            Win32.INPUT input = new Win32.INPUT();
            input.type = Win32.INPUT_KEYBOARD;
            input.U.ki.wVk = bVk;
            input.U.ki.wScan = bScan;
            input.U.ki.time = 0;
            input.U.ki.dwExtraInfo = IntPtr.Zero;

            if (bScan == 0)
                input.U.ki.dwFlags = (uint)(isPress ? 0 : Win32.KEYEVENTF_KEYUP);
            else
                input.U.ki.dwFlags = (uint)Win32.KEYEVENTF_SCANCODE | (uint)(isPress ? 0 : Win32.KEYEVENTF_KEYUP);

            uint sent = Win32.SendInput(1, new Win32.INPUT[] { input }, Marshal.SizeOf(typeof(Win32.INPUT)));
            if (sent == 0)
            {
                //实在发不出去就退回旧接口
                Win32.keybd_event(bVk, bScan, (uint)(isPress ? Win32.KEYEVENTF_KEYDOWN : Win32.KEYEVENTF_KEYUP), UIntPtr.Zero);
            }
        }

        #endregion
    }
}
