using MaterialDesignThemes.Wpf;
using SkyPC_AutoMusic.Command;
using SkyPC_AutoMusic.Event;
using SkyPC_AutoMusic.Event.Options;
using SkyPC_AutoMusic.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AppTheme = SkyPC_AutoMusic.Theme;

namespace SkyPC_AutoMusic.ViewModel
{
    internal class OptionsViewModel : NotificationObject
    {
        private string url = "https://github.com/GUKUAN/SkyPC_AutoMusic";

        private Settings settings;

        private ComboBox LanguageComboBox;

        //乐谱文件夹路径
        public string FolderPath
        {
            get { return settings.FolderPath; }
            set 
            {
                settings.FolderPath = value;
                OnPropertyChanged();
                Save();
            }
        }

        //预设主题列表
        public List<AppTheme.ThemeEntry> ThemeList
        {
            get { return AppTheme.Themes.Presets.ToList(); }
        }

        //当前主题
        public AppTheme.ThemeEntry SelectedTheme
        {
            get { return AppTheme.Themes.Current; }
            set
            {
                if (value == null)
                    return;
                AppTheme.Themes.SetCurrent(value.Id);
                AppTheme.ThemeService.ApplyCurrent(true);
                settings.ThemeId = value.Id;
                OnPropertyChanged();
                Save();
            }
        }

        //自定义背景图像
        public bool UserImageBackground
        {
            get { return settings.UserImageBackground; }
            set
            {
                settings.UserImageBackground = value;
                ChangeBackground(value,true);
                OnPropertyChanged();
                Save();
            }
        }

        //延迟释放按键
        public bool DelayToReleaseKey
        {
            get { return settings.DelayToReleaseKey; }
            set
            {
                settings.DelayToReleaseKey = value;
                EA.EventAggregator.GetEvent<SwitchDelayToReleaseEvent>().Publish(value);
                if (value)
                    SendDialog.MessageTips(Properties.Resources.Options_Tips_CombineKeys);
                OnPropertyChanged();
                Save();
            }
        }

        //后台演奏
        public bool IsPlayInBackground
        {
            get { return settings.isPlayInBackground; }
            set
            {
                settings.isPlayInBackground = value;
                EA.EventAggregator.GetEvent<SwitchPlayBackgroundEvent>().Publish(value);
                OnPropertyChanged();
                Save();
            }
        }

        //键位映射
        public bool IsUsingSkyStudioKeyMapper
        {
            get { return settings.isUsingSkyStudioKeyMapper; }
            set
            {
                settings.isUsingSkyStudioKeyMapper = value;
                EA.EventAggregator.GetEvent<SwitchSkyStudioKeyMapperEvent>().Publish(value);
                OnPropertyChanged();
                Save();
            }
        }

        //自定义键位开关
        public bool UseCustomKeyMapper
        {
            get { return settings.UseCustomKeyMapper; }
            set
            {
                settings.UseCustomKeyMapper = value;
                OnPropertyChanged();
                Save();
            }
        }

        //递归导入子文件夹
        public bool ImportSubfolders
        {
            get { return settings.ImportSubfolders; }
            set
            {
                settings.ImportSubfolders = value;
                OnPropertyChanged();
                Save();
            }
        }

        //全局热键
        public bool UseHotkeys
        {
            get { return settings.UseHotkeys; }
            set
            {
                settings.UseHotkeys = value;
                EA.EventAggregator.GetEvent<SwitchHotkeysEvent>().Publish(value);
                if (value)
                    EA.EventAggregator.GetEvent<SendMessageSnackbar>().Publish(Properties.Resources.Options_Tips_Hotkeys);
                OnPropertyChanged();
                Save();
            }
        }

        //GitHub Token（可选）
        public string GitHubToken
        {
            get { return settings.GitHubToken; }
            set
            {
                settings.GitHubToken = value;
                OnPropertyChanged();
                Save();
            }
        }

        //版本号（从程序集读取，避免写死在界面里）
        public string VersionText
        {
            get
            {
                Version version = Assembly.GetExecutingAssembly().GetName().Version;
                return "Version " + version.ToString(3);
            }
        }

        public DelegateCommand OpenWebPageCommand { get; set; }
        public DelegateCommand LanguageChangedCommand { get; set; }
        public DelegateCommand OpenKeybindCommand { get; set; }

        public OptionsViewModel(ComboBox languageComboBox)
        {
            //读取设置
            settings = Settings.Instance;
            Read();
            //语言框
            LanguageComboBox = languageComboBox;
            var languages = new List<LanguageItem>
            {
                new LanguageItem { DisplayName = "Auto", LanguageCode = null },
                new LanguageItem { DisplayName = "中文", LanguageCode = "zh-CN" },
                new LanguageItem { DisplayName = "English", LanguageCode = "en-US" }
            };
            LanguageComboBox.ItemsSource = languages;
            LanguageComboBox.SelectedIndex = languages.FindIndex(l => l.LanguageCode == settings.LanguageCode);
            //命令绑定
            OpenWebPageCommand = new DelegateCommand(OpenWebPage);
            LanguageChangedCommand = new DelegateCommand(LanguageComboBoxChanged);
            OpenKeybindCommand = new DelegateCommand(OpenKeybindDialog);
            EA.EventAggregator.GetEvent<SaveFolderPathEvent>().Subscribe(SaveFolderPath);
            //应用读取的设置
            ApplyAllOptions();
        }

        //读取设置（启动时已统一加载，这里再同步一次，保证单例最新）
        private void Read()
        {
            Settings.Load();
        }

        //保存设置
        private void Save()
        {
            Settings.Save();
        }

        //将乐谱文件夹写入设置
        private void SaveFolderPath(string path)
        {
            FolderPath = path;
        }

        //应用所有设置
        private void ApplyAllOptions()
        {
            //从文件夹导入乐谱
            if (FolderPath != null)
            {
                if (Directory.Exists(FolderPath))
                {
                    EA.EventAggregator.GetEvent<SelectFolderWithPathEvent>().Publish(FolderPath);
                }
                else
                {
                    EA.EventAggregator.GetEvent<SendMessageSnackbar>().Publish(Properties.Resources.Options_ReadPathFailure);
                }
            }
            //初始化键位
            EA.EventAggregator.GetEvent<SwitchSkyStudioKeyMapperEvent>().Publish(IsUsingSkyStudioKeyMapper);
            //初始化延音
            EA.EventAggregator.GetEvent<SwitchDelayToReleaseEvent>().Publish(DelayToReleaseKey);
            //初始化后台演奏
            EA.EventAggregator.GetEvent<SwitchPlayBackgroundEvent>().Publish(IsPlayInBackground);
            //初始化热键
            EA.EventAggregator.GetEvent<SwitchHotkeysEvent>().Publish(UseHotkeys);
            //背景图像
            ChangeBackground(UserImageBackground,false);
        }

        //切换背景图像
        private void ChangeBackground(bool useBackground,bool sendFailureMessage)
        {
            string imgPath = null;
            foreach (string candidate in AppPath.BackgroundImages)
            {
                if (File.Exists(candidate))
                {
                    imgPath = candidate;
                    break;
                }
            }

            if (useBackground)//添加背景图片
            {
                if (imgPath != null)//图片存在
                {
                    //读取图片
                    BitmapImage bitmap;
                    using (FileStream fs = new FileStream(imgPath, FileMode.Open))
                    {
                        bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = fs;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                    }
                    //切换背景图像
                    EA.EventAggregator.GetEvent<AddBackgroundEvent>().Publish(bitmap);
                }
                else//图片不存在
                {
                    if (sendFailureMessage)
                    {
                        SendDialog.MessageTips(Properties.Resources.Options_Tips_MissBackgroundFile);
                    }
                }
            }
            else//删除背景图片
            {
                EA.EventAggregator.GetEvent<DeleteBackgroundEvent>().Publish();
            }
        }

        //打开网页
        private void OpenWebPage()
        {
            Process.Start(new ProcessStartInfo(url)
            {
                UseShellExecute = true,
                Verb = "open"
            });
        }

        //打开自定义键位对话框
        private void OpenKeybindDialog()
        {
            //暂停
            if (PlayViewModel.Instance != null && PlayViewModel.Instance.IsPlaying())
            {
                EA.EventAggregator.GetEvent<PauseSongEvent>().Publish();
            }
            DialogHost.Show(new SkyPC_AutoMusic.View.UserControlKeybindDialog(), "RootDialog");
            MainWindow.Instance.Activate();
        }

        //切换语言
        private void LanguageComboBoxChanged()
        {
            ResourceManager resourceManager = new ResourceManager("SkyPC_AutoMusic.Properties.Resources",typeof(OptionsViewModel).Assembly);
            LanguageItem selectedItem = (LanguageItem)LanguageComboBox.SelectedItem;
            string tips;
            if (selectedItem.LanguageCode != null)
            {
                tips = resourceManager.GetString("Options_Tips_RestartApp", new CultureInfo(selectedItem.LanguageCode));
            }
            else
            {
                tips = Properties.Resources.Options_Tips_RestartApp;
            }
            EA.EventAggregator.GetEvent<SendMessageSnackbar>().Publish(tips);
            settings.LanguageCode = selectedItem.LanguageCode;
            Save();
        }

    }
}
