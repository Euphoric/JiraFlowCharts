using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
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
using LiveCharts;
using LiveCharts.Configurations;
using Newtonsoft.Json;

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

            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var mapper1 = Mappers.Xy<CycleTimeScatterplotViewModel.IssuePoint>()
                .X(value => value.X)
                .Y(value => value.Y);
            LiveCharts.Charting.For<CycleTimeScatterplotViewModel.IssuePoint>(mapper1);

            var issues = RetrieveIssues();


            DateTime startDate = DateTime.Now.AddMonths(-12);

            var stories = issues.Where(x => x.Type == "Story" || x.Type == "Bug");

            var finishedStories = stories
                .Where(x=>x.Resolution != "Cancelled" && x.Status == "Done")
                .Select(CalculateDuration)
                .Where(x=>x.Duration.HasValue && x.Duration.Value < 80)
                .Where(x=>x.End > startDate).ToArray();

            CycleTimeScatterplotTab.DataContext = new CycleTimeScatterplotViewModel(finishedStories);
            CycleTimeHistogram.DataContext = new CycleTimeHistogramViewModel(finishedStories);
            StoryPointCycleTime.DataContext = new StoryPointCycleTimeViewModel(finishedStories);
            CumulativeFlow.DataContext = new CumulativeFlowViewModel(stories);
        }

        private static FlowIssue CalculateDuration(FlatIssue issue)
        {
            var startTime = issue.StatusChanges.FirstOrDefault(x=>x.State == "In Dev")?.ChangeTime;
            var doneTime = issue.StatusChanges.LastOrDefault(x=>x.State == "Done")?.ChangeTime;

            TimeSpan? duration = doneTime - startTime;

            var flowIssue = new FlowIssue()
            {
                Key = issue.Key,
                Title = issue.Title,
                Type = issue.Type,
                Start = startTime,
                End = doneTime,
                Duration = duration?.TotalDays,
                StoryPoints = issue.StoryPoints,
                TimeSpent = issue.TimeSpent
            };

            return flowIssue;
        }

        private static List<FlatIssue> RetrieveIssues()
        {
            using (StreamReader r = new StreamReader("../../../Data/issues.json"))
            using (JsonReader jr = new JsonTextReader(r))
            {
                JsonSerializer serializer = new JsonSerializer();
                return serializer.Deserialize<List<FlatIssue>>(jr);
            }
        }
    }
}
