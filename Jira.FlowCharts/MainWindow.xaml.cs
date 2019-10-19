using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LiveCharts;
using LiveCharts.Configurations;

namespace Jira.FlowCharts
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var mapper1 = Mappers.Xy<CycleTimeScatterplotViewModel.IssuePoint>()
                .X(value => value.X)
                .Y(value => value.Y);
            LiveCharts.Charting.For<CycleTimeScatterplotViewModel.IssuePoint>(mapper1);

            MainViewModel mainViewModel = new MainViewModel();
            await mainViewModel.Initialize();
            DataContext = mainViewModel;
        }
    }
}
