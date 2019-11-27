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

namespace Jira.FlowCharts.IssuesGrid
{
    /// <summary>
    /// Interaction logic for IssuesGridView.xaml
    /// </summary>
    public partial class IssuesGridView : UserControl
    {
        public IssuesGridView()
        {
            InitializeComponent();

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            IssuesGrid.Columns.Clear();

            IssuesGrid.Columns.Add(new DataGridTextColumn { Header = "Key", Binding = new Binding("Key") });
            IssuesGrid.Columns.Add(new DataGridTextColumn { Header = "Title", Binding = new Binding("Title"), Width = 120 });
            IssuesGrid.Columns.Add(new DataGridTextColumn { Header = "Type", Binding = new Binding("Type") });
            IssuesGrid.Columns.Add(new DataGridTextColumn { Header = "Status", Binding = new Binding("Status") });
            IssuesGrid.Columns.Add(new DataGridTextColumn { Header = "Resolution", Binding = new Binding("Resolution") });
            IssuesGrid.Columns.Add(new DataGridCheckBoxColumn() { Header = "IsValid", Binding = new Binding("IsValid") });
            IssuesGrid.Columns.Add(new DataGridTextColumn { Header = "Story Points", Binding = new Binding("StoryPoints") });
            IssuesGrid.Columns.Add(new DataGridTextColumn { Header = "Time estimated", Binding = new Binding("OriginalEstimate") { StringFormat = "N1" }, Width = 50});
            IssuesGrid.Columns.Add(new DataGridTextColumn { Header = "Time spent", Binding = new Binding("TimeSpent"){StringFormat = "N1"}, Width = 50 });

            IssuesGrid.Columns.Add(new DataGridTextColumn { Header = "Started", Binding = new Binding("Started"){StringFormat = "u"} });
            IssuesGrid.Columns.Add(new DataGridTextColumn { Header = "Ended", Binding = new Binding("Ended") { StringFormat = "u" } });
            IssuesGrid.Columns.Add(new DataGridTextColumn { Header = "Duration", Binding = new Binding("DurationDays"){StringFormat = "N1"} });
        }
    }
}
