using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SkyPC_AutoMusic.Model
{

    public class Settings
    {
        //单例
        private static readonly Settings instance = new Settings();

        private Settings() { }

        static Settings() { }

        public static Settings Instance
        {
            get { return instance; }
        }

        //私有变量
        private string folderPath;
        private string languageCode;
        private bool darkTheme;
        private bool themeColorFollowSystem;
        private bool userImageBackground;
        private bool delayToReleaseKey;
        private bool playInBackground;
        private bool skyStudioKeyMapper;
        //新增：播放列表 / 键位 / 热键
        private List<string> sheetPaths;
        private bool importSubfolders;
        private bool useCustomKeyMapper;
        private List<int> customKeys;
        private bool useHotkeys;
        private bool playlistInitialized;
        private string gitHubToken;
        private HumanizeSettings humanize;
        private string themeId;

        //文件夹路径
        public string FolderPath
        {
            get { return folderPath; }
            set { folderPath = value; }
        }

        //语言
        public string LanguageCode
        {
            get { return languageCode; }
            set { languageCode = value; }
        }

        //深色模式
        public bool DarkTheme
        {
            get { return darkTheme; }
            set { darkTheme = value; }
        }

        //主题色跟随系统
        public bool ThemeColorFollowSystem
        {
            get { return themeColorFollowSystem; }
            set { themeColorFollowSystem = value; }
        }

        //自定义背景图像
        public bool UserImageBackground
        {
            get { return userImageBackground; }
            set { userImageBackground = value; }
        }

        //按键延音
        public bool DelayToReleaseKey
        {
            get { return delayToReleaseKey; }
            set { delayToReleaseKey = value; }
        }

        //后台播放
        public bool isPlayInBackground
        {
            get { return playInBackground; }
            set { playInBackground = value; }
        }

        //键位映射
        public bool isUsingSkyStudioKeyMapper
        {
            get { return skyStudioKeyMapper; }
            set { skyStudioKeyMapper = value; }
        }

        //播放列表（保存每条乐谱的文件路径，重启后原样恢复）
        public List<string> SheetPaths
        {
            get
            {
                if (sheetPaths == null)
                    sheetPaths = new List<string>();
                return sheetPaths;
            }
            set { sheetPaths = value; }
        }

        //导入时是否递归子文件夹
        public bool ImportSubfolders
        {
            get { return importSubfolders; }
            set { importSubfolders = value; }
        }

        //是否使用自定义键位
        public bool UseCustomKeyMapper
        {
            get { return useCustomKeyMapper; }
            set { useCustomKeyMapper = value; }
        }

        //自定义键位（15 个虚拟键值，下标即音符序号）
        public List<int> CustomKeys
        {
            get
            {
                if (customKeys == null || customKeys.Count != 15)
                    customKeys = DefaultCustomKeys();
                return customKeys;
            }
            set { customKeys = value; }
        }

        //是否启用全局热键
        public bool UseHotkeys
        {
            get { return useHotkeys; }
            set { useHotkeys = value; }
        }

        //播放列表是否已经初始化过（初始化后不再自动扫描旧文件夹）
        public bool PlaylistInitialized
        {
            get { return playlistInitialized; }
            set { playlistInitialized = value; }
        }

        //GitHub Token（可选，提升市场清单请求限额）
        public string GitHubToken
        {
            get { return gitHubToken; }
            set { gitHubToken = value; }
        }

        //当前主题（预设 id）
        public string ThemeId
        {
            get { return String.IsNullOrEmpty(themeId) ? Theme.Themes.DefaultId : themeId; }
            set { themeId = value; }
        }

        //拟人化设置（保持同一实例，界面绑定不失效）
        public HumanizeSettings Humanize
        {
            get
            {
                if (humanize == null)
                    humanize = new HumanizeSettings();
                return humanize;
            }
            set { humanize = value; }
        }

        //从配置文件读到单例（程序启动时调用，和界面是否打开无关）
        public static void Load()
        {
            if (!File.Exists(AppPath.Settings))
                return;
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Settings));
                using (StreamReader reader = new StreamReader(AppPath.Settings))
                {
                    Settings loaded = (Settings)serializer.Deserialize(reader);
                    Instance.CopyFrom(loaded);
                }
            }
            catch
            {
                //设置文件坏了就当默认值用
            }
        }

        //把另一份设置的各项拷进当前单例（保持单例与界面对象不变）
        public void CopyFrom(Settings source)
        {
            if (source == null)
                return;
            FolderPath = source.FolderPath;
            LanguageCode = source.LanguageCode;
            DarkTheme = source.DarkTheme;
            ThemeColorFollowSystem = source.ThemeColorFollowSystem;
            UserImageBackground = source.UserImageBackground;
            DelayToReleaseKey = source.DelayToReleaseKey;
            isPlayInBackground = source.isPlayInBackground;
            isUsingSkyStudioKeyMapper = source.isUsingSkyStudioKeyMapper;
            SheetPaths = source.SheetPaths;
            ImportSubfolders = source.ImportSubfolders;
            UseCustomKeyMapper = source.UseCustomKeyMapper;
            CustomKeys = source.CustomKeys;
            UseHotkeys = source.UseHotkeys;
            PlaylistInitialized = source.PlaylistInitialized;
            GitHubToken = source.GitHubToken;
            ThemeId = source.ThemeId;
            Humanize.CopyFrom(source.Humanize);
        }

        //把当前单例写到配置文件
        public static void Save()
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Settings));
                using (StreamWriter writer = new StreamWriter(AppPath.Settings))
                {
                    serializer.Serialize(writer, Instance);
                }
            }
            catch
            {
                //写不进去就算了，别因为配置文件崩程序
            }
        }

        //默认自定义键位（沿用天空默认的 YUIOP/HJKL;/NM,./ 布局）
        public static List<int> DefaultCustomKeys()
        {
            return new List<int>
            {
                0x59, 0x55, 0x49, 0x4F, 0x50,
                0x48, 0x4A, 0x4B, 0x4C, 0xBA,
                0x4E, 0x4D, 0xBC, 0xBE, 0xBF
            };
        }
    }
}
