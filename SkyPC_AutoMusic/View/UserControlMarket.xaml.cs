using SkyPC_AutoMusic.Model;
using SkyPC_AutoMusic.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SkyPC_AutoMusic.View
{
    /// <summary>
    /// UserControlMarket.xaml 的交互逻辑
    /// </summary>
    public partial class UserControlMarket : UserControl
    {
        public UserControlMarket()
        {
            InitializeComponent();
            DataContext = new MarketViewModel();
        }

        //列表里每行的“下载并导入”
        private void Download_Click(object sender, RoutedEventArgs e)
        {
            MarketViewModel model = DataContext as MarketViewModel;
            MarketItem item = (sender as Button)?.DataContext as MarketItem;
            if (model == null || item == null)
                return;

            model.DownloadItem(item);
        }
    }
}
