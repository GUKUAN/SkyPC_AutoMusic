using SkyPC_AutoMusic.Command;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using Prism.Events;
using SkyPC_AutoMusic.Event;
using SkyPC_AutoMusic.Model;
using MaterialDesignThemes.Wpf;
using System.Windows.Input;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using SkyPC_AutoMusic.View;
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows;
using System.Threading;

namespace SkyPC_AutoMusic.ViewModel
{
    internal class ListViewModel : NotificationObject
    {
        private string filterText = String.Empty;
        private System.Windows.Controls.ListView listView;

        #region 公开属性

        private ObservableCollection<Song> sheetList;

        public ObservableCollection<Song> SheetList
        {
            get { return sheetList; }
        }

        private Song selectedItem;

        public Song SelectedItem
        {
            get { return selectedItem; }
            set { selectedItem = value; }
        }

        public string HeaderText
        {
            get { return Properties.Resources.List_Header_SheetsList + "(" + SheetList.Count.ToString() + ")"; }
        }

        public DelegateCommand AddSongCommand { get; set; }

        public DelegateCommand DeleteSelectionCommand { get; set; }

        public DelegateCommand SelectFolderCommand { get; set; }

        public DelegateCommand SelectionChangeCommand { get; set; }

        public DelegateCommand FilterDialogCommand { get; set; }
        #endregion

        public ListViewModel(System.Windows.Controls.ListView listView)
        {
            this.listView = listView;
            //私有变量
            EA.EventAggregator.GetEvent<SongSwitchEvent>().Subscribe(SwitchCurrentSong);
            EA.EventAggregator.GetEvent<SelectFolderWithPathEvent>().Subscribe(AutoSelectFolder);
            EA.EventAggregator.GetEvent<SetFilterEvent>().Subscribe(SetFilter);
            EA.EventAggregator.GetEvent<AddFilesEvent>().Subscribe(ImportDroppedFiles);
            //公开属性
            sheetList = new ObservableCollection<Song>();
            //命令
            DeleteSelectionCommand = new DelegateCommand(DeleteSelection);
            SelectFolderCommand = new DelegateCommand(SelectFolder);
            SelectionChangeCommand = new DelegateCommand(SelectionChange);
            AddSongCommand = new DelegateCommand(AddSong);
            FilterDialogCommand = new DelegateCommand(OpenFilterDialog);
            SheetList.CollectionChanged += (sender, e) => { OnPropertyChanged("HeaderText"); };
            //恢复上次导入的播放列表
            RestorePlaylist();
        }

        #region 私有方法

        private void SetFilter(string filterString)
        {
            filterText = filterString;
            listView.Items.Filter = FilterMethod;
        }

        private bool FilterMethod(object obj)
        {
            var sheet = (Song)obj;

            //return sheet.name.Contains(filterText, StringComparison.OrdinalIgnoreCase);
            return sheet.name.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) > -1;
        }

        private void SelectionChange()
        {
            //获取选中乐谱的索引
            int songIndex = sheetList.IndexOf(SelectedItem);
            if (songIndex != -1)
            {
                EA.EventAggregator.GetEvent<SongSwitchWithIndexEvent>().Publish(songIndex);
            }
        }

        private void SwitchCurrentSong(Player player)
        {
            if (sheetList.Count == 0)//列表空了就别切了
                return;
            if (player.currentSheetIndex < 0 || player.currentSheetIndex >= sheetList.Count)
                return;

            //切歌
            player.currentSong = sheetList[player.currentSheetIndex];
            //更新标题
            EA.EventAggregator.GetEvent<HeadlineSwitchEvent>().Publish(sheetList[player.currentSheetIndex].name);
        }

        private void SelectFolder()
        {
            //暂停
            if (PlayViewModel.Instance != null && PlayViewModel.Instance.IsPlaying())
            {
                EA.EventAggregator.GetEvent<PauseSongEvent>().Publish();
            }
            //打开对话框
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                ReadSheetsFromFolder(dialog.SelectedPath, true);
                EA.EventAggregator.GetEvent<SaveFolderPathEvent>().Publish(dialog.SelectedPath);
            }
        }

        private void ReadSheetsFromFolder(string folderPath, bool showDialog)
        {
            //清理列表
            sheetList.Clear();
            //按设置决定是否递归子文件夹
            SearchOption option = Settings.Instance.ImportSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            List<string> files;
            try
            {
                files = Directory.GetFiles(folderPath, "*", option).ToList();
            }
            catch (Exception e)
            {
                SendDialog.MessageTips(e.Message);
                //启用列表视图
                EA.EventAggregator.GetEvent<EnableListEvent>().Publish(true);
                return;
            }
            ImportFiles(files, showDialog, true);
        }

        //把一批文件导入到列表里
        private void ImportFiles(List<string> files, bool showDialog, bool announce)
        {
            //反馈信息
            int successCount = 0;
            int failCount = 0;
            string failureList = String.Empty;
            int total = 0;
            int totalCount = files.Count(file => Path.GetExtension(file).ToLower() == ".txt");
            //禁用列表视图
            EA.EventAggregator.GetEvent<EnableListEvent>().Publish(false);
            //没有可导入文件时直接返回
            if (totalCount == 0)
            {
                string infoEmpty = string.Format(Properties.Resources.List_ImportResult, successCount, failCount);
                if (announce)
                {
                    SendDialog.MessageTips(infoEmpty);
                }
                //启用列表视图
                EA.EventAggregator.GetEvent<EnableListEvent>().Publish(true);
                return;
            }
            //显示等待框
            if (showDialog)
            {
                SendDialog.WaitTips(Properties.Resources.List_ImportingFiles, totalCount);
            }
            else if (announce)
            {
                EA.EventAggregator.GetEvent<SendMessageSnackbar>().Publish(String.Format(Properties.Resources.List_ImportingInBackground, totalCount));
            }
            Task.Run(() =>
            {
                //遍历文件
                foreach (string file in files)
                {
                    if (Path.GetExtension(file).ToLower() == ".txt")
                    {
                        string error;
                        Song song;
                        //从文件实例化类
                        song = ReadJson(file, out error);
                        if (song != null)//成功
                        {
                            //计数
                            successCount++;
                            //添加至列表（同路径的曲谱不重复导入）
                            if (System.Windows.Application.Current != null)
                            {
                                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                {
                                    bool exists = SheetList.Any(s => String.Equals(s.sourcePath, song.sourcePath, StringComparison.OrdinalIgnoreCase));
                                    if (!exists)
                                        SheetList.Add(song);
                                });
                            }
                        }
                        else//失败
                        {
                            failCount++;
                            failureList += ("\n\n" + file + "\n" + Properties.Resources.List_ImportFailureReason + ": " + (error ?? String.Empty));
                        }

                        total++;
                        EA.EventAggregator.GetEvent<PassProgressToWaitDialogEvent>().Publish(total);
                    }
                }

            }).ContinueWith((preTask) =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    //启用列表视图
                    EA.EventAggregator.GetEvent<EnableListEvent>().Publish(true);
                    //反馈信息
                    string infoFeedback = string.Format(Properties.Resources.List_ImportResult, successCount, failCount);
                    if (showDialog || announce)
                    {
                        if (failCount != 0)
                        {
                            infoFeedback += "\n\n" + Properties.Resources.List_ImportFailureTips + "\n\n" + Properties.Resources.List_ImportFailureList;
                            infoFeedback += failureList;
                        }
                        if (showDialog)
                        {
                            SendDialog.MessageTips(infoFeedback);
                        }
                        else
                        {
                            EA.EventAggregator.GetEvent<SendMessageSnackbar>().Publish(infoFeedback);
                        }
                    }
                    FinishImport();
                });
            });
        }

        //一次导入结束后的收尾：存盘 + 通知数量/切歌
        private void FinishImport()
        {
            SavePlaylist();
            if (sheetList.Count > 0)
            {
                //传递乐谱数
                EA.EventAggregator.GetEvent<PassSheetsCountEvent>().Publish(sheetList.Count);
                //切歌
                EA.EventAggregator.GetEvent<FolderSwitchEvent>().Publish();
            }
            else
            {
                //列表空了也要同步数量，否则“下一首”会越界
                EA.EventAggregator.GetEvent<PassSheetsCountEvent>().Publish(0);
            }
        }

        //启动时恢复上次的播放列表
        private void RestorePlaylist()
        {
            List<string> paths = Settings.Instance.SheetPaths;
            if (paths.Count > 0)
            {
                ImportFiles(paths.ToList(), false, false);
            }
        }

        //把当前列表里的文件路径写入设置
        private void SavePlaylist()
        {
            Settings.Instance.SheetPaths = sheetList
                .Select(song => song.sourcePath)
                .Where(path => !String.IsNullOrEmpty(path))
                .ToList();
            Settings.Instance.PlaylistInitialized = true;
            Settings.Save();
        }

        private Song ReadJson(string path, out string error)
        {
            error = null;
            Sheet sheet;
            Song song;

            try
            {
                // 读取文件内容  
                string rawJson = File.ReadAllText(path);
                // 判断是否加密
                if (rawJson.Contains("\"isEncrypted\":true") || rawJson.Contains("\"isEncrypted\": true"))
                {
                    error = Properties.Resources.List_ImportEncrypted;
                    return null;
                }
                // 掐头掐尾
                int startIndex, endIndex;
                startIndex = rawJson.IndexOf('{');
                endIndex = rawJson.LastIndexOf('}');
                if (startIndex != -1 && endIndex != -1 && endIndex > startIndex)
                {
                    string json = rawJson.Substring(startIndex, endIndex - startIndex + 1);

                    // 反序列化成C#对象（兼容 SkyStudio/Nightly 与 VSRG）
                    sheet = SheetParser.Parse(json);
                    song = ConvertSheetToSong(sheet);
                    if (song != null)
                    {
                        //记录来源路径，方便下次恢复
                        song.sourcePath = path;
                    }

                    return song;
                }
                else
                {
                    // 读不到Json块
                    error = Properties.Resources.List_ImportFailureTips;
                    return null;
                }
            }
            catch (Exception e)
            {
                error = e.Message;
                Console.WriteLine(e.Message);
                return null;
            }
        }

        private void AddSong()
        {
            //暂停
            if (PlayViewModel.Instance != null && PlayViewModel.Instance.IsPlaying())
            {
                EA.EventAggregator.GetEvent<PauseSongEvent>().Publish();
            }
            //打开对话框
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = Properties.Resources.List_ImportWindowTitle;
            dialog.Filter = Properties.Resources.List_TextFile + "(*.txt)|*.txt";
            //反馈信息
            string infoFeedback = string.Empty;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string error;
                Song song;
                //从文件实例化类
                song = ReadJson(dialog.FileName, out error);
                if (song != null)
                {
                    //添加至列表
                    SheetList.Add(song);
                    infoFeedback = song.name + " " + Properties.Resources.List_ImportSuccessfully;
                }
                else
                {
                    infoFeedback = Properties.Resources.List_ImportFailure + "\n\n" + (error ?? Properties.Resources.List_ImportFailureTips);
                }
                if (sheetList.Count > 0)
                {
                    //传递乐谱数
                    EA.EventAggregator.GetEvent<PassSheetsCountEvent>().Publish(sheetList.Count);
                    //切歌
                    EA.EventAggregator.GetEvent<FolderSwitchEvent>().Publish();
                }
                SavePlaylist();
                SendDialog.MessageTips(infoFeedback);
            }
        }

        //拖拽文件导入
        private void ImportDroppedFiles(string[] paths)
        {
            if (paths == null || paths.Length == 0)
                return;
            //暂停
            if (PlayViewModel.Instance != null && PlayViewModel.Instance.IsPlaying())
            {
                EA.EventAggregator.GetEvent<PauseSongEvent>().Publish();
            }
            ImportFiles(paths.ToList(), false, true);
        }

        private void DeleteSelection()
        {
            if (SelectedItem == null)
            {
                SendDialog.MessageTips(Properties.Resources.List_NullDelete);
                return;
            }

            //暂停，避免正播着被删掉
            if (PlayViewModel.Instance != null && PlayViewModel.Instance.IsPlaying())
            {
                EA.EventAggregator.GetEvent<PauseSongEvent>().Publish();
            }

            int songIndex = sheetList.IndexOf(SelectedItem) - 1;
            SheetList.Remove(SelectedItem);
            //同步数量，防止“下一首”越界
            EA.EventAggregator.GetEvent<PassSheetsCountEvent>().Publish(sheetList.Count);

            if (sheetList.Count == 0)
            {
                SelectedItem = null;
                SavePlaylist();
                return;
            }

            if (songIndex > -1)
            {
                SelectedItem = sheetList[songIndex];
                EA.EventAggregator.GetEvent<SongSwitchWithIndexEvent>().Publish(songIndex);
            }
            else
            {
                //删的是第一首，切到新的第一首
                SelectedItem = sheetList[0];
                EA.EventAggregator.GetEvent<SongSwitchWithIndexEvent>().Publish(0);
            }
            SavePlaylist();
        }

        private void AutoSelectFolder(string folderPath)
        {
            //播放列表已经初始化过就不再自动扫描旧文件夹
            if (Settings.Instance.PlaylistInitialized)
                return;
            //已经存了播放列表也别扫描，免得把恢复中的列表冲掉
            if (Settings.Instance.SheetPaths.Count > 0)
                return;
            ReadSheetsFromFolder(folderPath, false);
        }

        private Song ConvertSheetToSong(Sheet sheet)
        {
            //检查属性
            if (sheet.bitsPerPage == 0)
            {
                sheet.bitsPerPage = 16;
            }
            if (sheet.bpm == 0)
            {
                sheet.bpm = 240;
            }

            try
            {
                //基本属性
                Song song = new Song
                {
                    name = sheet.name,
                    author = sheet.author,
                    transcribedBy = sheet.transcribedBy,
                    isComposed = sheet.isComposed,
                    bpm = sheet.bpm,
                    bitsPerPage = sheet.bitsPerPage,
                    pitchLevel = sheet.pitchLevel,
                    isEncrypted = sheet.isEncrypted,
                    Beats = new List<Beat>()
                };

                //每一拍的时间间隔
                int singleBeatInterval = 60000 / sheet.bpm;

                //拍数
                int LastBeatsCount = (sheet.songNotes.Max(item => item.time) + singleBeatInterval) / singleBeatInterval;//sheet.songNotes[sheet.songNotes.Count - 1].time / singleBeatInterval;
                int totalBeatsCount = ((LastBeatsCount + song.bitsPerPage - 1) / song.bitsPerPage) * song.bitsPerPage;

                //为每个节拍赋值
                for (int i = 0; i < totalBeatsCount; i++)
                {
                    //实例化单个节拍
                    Beat beat = new Beat();
                    beat.Keys = new List<NoteKey>();
                    //单个节拍的开始时间
                    beat.Time = singleBeatInterval * i;

                    //单个节拍内的按键
                    foreach (SongNote note in sheet.songNotes)
                    {
                        if (note.time >= beat.Time && note.time < beat.Time + singleBeatInterval)//单个按键时间位于节拍开始与节拍结束之间
                        {
                            //为节拍添加按键
                            NoteKey key = ConvertStringToEnum(note.key);
                            beat.Keys.Add(key);
                        }

                    }

                    //添加节拍
                    song.Beats.Add(beat);
                }

                return song;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        private NoteKey ConvertStringToEnum(string keyName)
        {
            string str = keyName.Insert(0, '_'.ToString());
            
            //特判
            if(str == "_1Key15")
            {
                str = "_1Key1";
            }
            else if (str[1] == 'K')
            {
                str = str.Insert(1,'1'.ToString());
            }
            else if (str[1] != '1' && str[1] >= '0' && str[1] <= '9')//键值名为其他数字
            {
                StringBuilder sb = new StringBuilder(str);
                sb[1] = '1';
                str = sb.ToString();
            }

            //将键名转换为枚举
            return (NoteKey)Enum.Parse(typeof(NoteKey), str);
        }

        private void OpenFilterDialog()
        {
            //暂停
            if(PlayViewModel.Instance != null && PlayViewModel.Instance.IsPlaying())
            {
                EA.EventAggregator.GetEvent<PauseSongEvent>().Publish();
            }
            //显示
            DialogHost.Show(new UserControlFilterDialog(filterText), "RootDialog");
            MainWindow.Instance.Activate();
        }
        #endregion
    }
}
