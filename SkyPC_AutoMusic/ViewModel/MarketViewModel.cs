using SkyPC_AutoMusic.Command;
using SkyPC_AutoMusic.Event;
using SkyPC_AutoMusic.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SkyPC_AutoMusic.ViewModel
{
    internal class MarketViewModel : NotificationObject
    {
        //一级分类展示顺序
        private static readonly string[] groupOrder = { "Songs", "Multi-Sheet Songs", "VSRG" };

        private readonly List<MarketItem> catalog = new List<MarketItem>();

        private ICollectionView view;

        private string searchText = String.Empty;
        private string selectedGroup;
        private string selectedSub;
        private string statusText;
        private bool isBusy;
        private MarketItem selectedItem;

        public ICollectionView View
        {
            get { return view; }
        }

        public ObservableCollection<string> Groups { get; private set; }

        public ObservableCollection<string> Subs { get; private set; }

        public string AllLabel
        {
            get { return Properties.Resources.Market_All; }
        }

        public string SearchText
        {
            get { return searchText; }
            set
            {
                searchText = value;
                RefreshFilter();
                OnPropertyChanged();
            }
        }

        public string SelectedGroup
        {
            get { return selectedGroup; }
            set
            {
                selectedGroup = value;
                RebuildSubs();
                RefreshFilter();
                OnPropertyChanged();
            }
        }

        public string SelectedSub
        {
            get { return selectedSub; }
            set
            {
                selectedSub = value;
                RefreshFilter();
                OnPropertyChanged();
            }
        }

        public MarketItem SelectedItem
        {
            get { return selectedItem; }
            set { selectedItem = value; OnPropertyChanged(); }
        }

        public string StatusText
        {
            get { return statusText; }
            private set { statusText = value; OnPropertyChanged(); }
        }

        public bool IsBusy
        {
            get { return isBusy; }
            private set { isBusy = value; OnPropertyChanged(); }
        }

        public DelegateCommand RefreshCommand { get; set; }
        public DelegateCommand SearchCommand { get; set; }
        public DelegateCommand DownloadAllCommand { get; set; }
        public DelegateCommand OpenRepoCommand { get; set; }

        public MarketViewModel()
        {
            Groups = new ObservableCollection<string> { AllLabel };
            Subs = new ObservableCollection<string> { AllLabel };
            selectedGroup = AllLabel;
            selectedSub = AllLabel;

            //用默认视图做筛选，避免复制上万条数据
            view = CollectionViewSource.GetDefaultView(catalog);
            view.Filter = FilterItem;

            RefreshCommand = new DelegateCommand(RefreshCatalog);
            SearchCommand = new DelegateCommand(() => { RefreshFilter(); });
            DownloadAllCommand = new DelegateCommand(DownloadVisible);
            OpenRepoCommand = new DelegateCommand(OpenRepository);

            LoadCache();
        }

        private void LoadCache()
        {
            DateTime fetchedAt;
            List<MarketItem> cached = MarketCatalog.LoadCache(out fetchedAt);
            if (cached == null || cached.Count == 0)
            {
                //没有清单就等用户点“刷新清单”
                StatusText = Properties.Resources.Market_StatusEmpty;
                return;
            }

            ReplaceCatalog(cached);
            StatusText = String.Format(Properties.Resources.Market_Status, catalog.Count, fetchedAt.ToString("yyyy-MM-dd HH:mm"));

            List<MarketItem> snapshot = catalog.ToList();
            Task.Run(() => MarketCatalog.RefreshDownloadedFlags(snapshot));
        }

        private async void RefreshCatalog()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            StatusText = Properties.Resources.Market_Fetching;
            try
            {
                List<MarketItem> items = await MarketCatalog.FetchAsync(Settings.Instance.GitHubToken);
                ReplaceCatalog(items);
                StatusText = String.Format(Properties.Resources.Market_Status, catalog.Count, DateTime.Now.ToString("yyyy-MM-dd HH:mm"));

                List<MarketItem> snapshot = catalog.ToList();
                Task.Run(() => MarketCatalog.RefreshDownloadedFlags(snapshot));
            }
            catch (Exception e)
            {
                StatusText = e.Message.Contains("403") ? Properties.Resources.Market_RateLimit : Properties.Resources.Market_FetchFailed;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ReplaceCatalog(List<MarketItem> items)
        {
            catalog.Clear();
            catalog.AddRange(items);
            RebuildGroups();
            RebuildSubs();
            RefreshFilter();
        }

        private void RebuildGroups()
        {
            Groups.Clear();
            Groups.Add(AllLabel);
            foreach (string group in catalog.Select(i => i.Group).Distinct()
                         .OrderBy(g => Array.IndexOf(groupOrder, g) < 0 ? int.MaxValue : Array.IndexOf(groupOrder, g)))
            {
                Groups.Add(group);
            }
            selectedGroup = AllLabel;
            OnPropertyChanged("SelectedGroup");
        }

        private void RebuildSubs()
        {
            Subs.Clear();
            Subs.Add(AllLabel);
            if (selectedGroup != AllLabel && selectedGroup != null)
            {
                foreach (string sub in catalog.Where(i => i.Group == selectedGroup)
                             .Select(i => i.Sub).Distinct().OrderBy(s => s, StringComparer.OrdinalIgnoreCase))
                {
                    Subs.Add(sub);
                }
            }
            selectedSub = AllLabel;
            OnPropertyChanged("SelectedSub");
        }

        private void RefreshFilter()
        {
            view.Refresh();
            OnPropertyChanged("EmptyVisible");
        }

        public bool EmptyVisible
        {
            get { return view.IsEmpty; }
        }

        private bool FilterItem(object obj)
        {
            MarketItem item = obj as MarketItem;
            if (item == null)
                return false;
            if (selectedGroup != null && selectedGroup != AllLabel && item.Group != selectedGroup)
                return false;
            if (selectedSub != null && selectedSub != AllLabel && item.Sub != selectedSub)
                return false;
            if (!String.IsNullOrWhiteSpace(searchText) && item.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) < 0)
                return false;
            return true;
        }

        //下载单曲并导入播放列表
        public async void DownloadItem(MarketItem item)
        {
            if (item == null || IsBusy)
                return;

            try
            {
                string local = await MarketCatalog.DownloadAsync(item, Settings.Instance.GitHubToken);
                EA.EventAggregator.GetEvent<AddFilesEvent>().Publish(new[] { local });
            }
            catch (Exception e)
            {
                EA.EventAggregator.GetEvent<SendMessageSnackbar>().Publish(Properties.Resources.Market_FetchFailed + ": " + e.Message);
            }
        }

        //把当前筛选结果全部下载并导入
        private async void DownloadVisible()
        {
            if (IsBusy)
                return;

            List<MarketItem> targets = view.Cast<MarketItem>().ToList();
            if (targets.Count == 0)
            {
                EA.EventAggregator.GetEvent<SendMessageSnackbar>().Publish(Properties.Resources.Market_Empty);
                return;
            }

            IsBusy = true;
            List<string> localFiles = new List<string>();
            int done = 0;
            try
            {
                foreach (MarketItem item in targets)
                {
                    done++;
                    StatusText = String.Format(Properties.Resources.Market_Downloading, done, targets.Count);
                    try
                    {
                        localFiles.Add(await MarketCatalog.DownloadAsync(item, Settings.Instance.GitHubToken));
                    }
                    catch
                    {
                        //单首失败就跳过，继续下一首
                    }
                }

                if (localFiles.Count > 0)
                    EA.EventAggregator.GetEvent<AddFilesEvent>().Publish(localFiles.ToArray());

                StatusText = String.Format(Properties.Resources.Market_ImportDone, localFiles.Count);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void OpenRepository()
        {
            Process.Start(new ProcessStartInfo("https://github.com/" + MarketCatalog.Repository)
            {
                UseShellExecute = true,
                Verb = "open"
            });
        }
    }
}
