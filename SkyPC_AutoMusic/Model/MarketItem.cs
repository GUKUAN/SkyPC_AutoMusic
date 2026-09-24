using System;
using System.ComponentModel;

namespace SkyPC_AutoMusic.Model
{
    //市场里的单条曲谱
    public class MarketItem : INotifyPropertyChanged
    {
        //仓库内的相对路径
        public string Path { get; set; }

        //文件名（去扩展名）
        public string Name { get; set; }

        //一级分类：Songs / Multi-Sheet Songs / VSRG
        public string Group { get; set; }

        //二级分类：字母或语言目录
        public string Sub { get; set; }

        public long Size { get; set; }

        //下载后的本地路径
        public string LocalPath { get; set; }

        private bool isDownloaded;
        public bool IsDownloaded
        {
            get { return isDownloaded; }
            set
            {
                if (isDownloaded == value)
                    return;
                isDownloaded = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("IsDownloaded"));
                    PropertyChanged(this, new PropertyChangedEventArgs("DownloadMark"));
                }
            }
        }

        //列表里显示的“已下载”标记
        public string DownloadMark
        {
            get { return isDownloaded ? Properties.Resources.Market_Downloaded : String.Empty; }
        }

        public string SizeText
        {
            get
            {
                if (Size >= 1024 * 1024)
                    return (Size / 1024.0 / 1024.0).ToString("0.0") + "MB";
                return (Size / 1024.0).ToString("0.0") + "KB";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
