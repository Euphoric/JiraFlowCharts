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
using Jira.Querying;
using Jira.Querying.Sqlite;
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

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var mapper1 = Mappers.Xy<CycleTimeScatterplotViewModel.IssuePoint>()
                .X(value => value.X)
                .Y(value => value.Y);
            LiveCharts.Charting.For<CycleTimeScatterplotViewModel.IssuePoint>(mapper1);

            var issues = await RetrieveIssues();

            DateTime startDate = DateTime.Now.AddMonths(-12);

            var stories = issues.Where(x => x.Type == "Story" || x.Type == "Bug");

            var states = new[] { "Ready For Dev", "In Dev", "Ready for Peer Review", "Ready for QA", "In QA", "Ready for Done", "Done" };
            var resetState = "On Hold";
            SimplifyStateChangeOrder simplify = new SimplifyStateChangeOrder(states, resetState);

            var finishedStories = stories
                .Where(x=>x.Resolution != "Cancelled" && x.Status == "Done")
                .Select(x=>CalculateDuration(x, simplify))
                .Where(x=>x.End > startDate).ToArray();

            CycleTimeScatterplotTab.DataContext = new CycleTimeScatterplotViewModel(finishedStories);
            CycleTimeHistogram.DataContext = new CycleTimeHistogramViewModel(finishedStories);
            StoryPointCycleTime.DataContext = new StoryPointCycleTimeViewModel(finishedStories);
            CumulativeFlow.DataContext = new CumulativeFlowViewModel(stories, states);
        }

        private static FlowIssue CalculateDuration(CachedIssue issue, SimplifyStateChangeOrder simplify)
        {
            var simplifiedIssues = simplify.FilterStatusChanges(issue.StatusChanges);

            var startTime = simplifiedIssues.First().ChangeTime;
            var doneTime = simplifiedIssues.Last().ChangeTime;

            TimeSpan duration = doneTime - startTime;

            var flowIssue = new FlowIssue()
            {
                Key = issue.Key,
                Title = issue.Title,
                Type = issue.Type,
                Start = startTime,
                End = doneTime,
                Duration = duration.TotalDays,
                StoryPoints = issue.StoryPoints,
                TimeSpent = issue.TimeSpent
            };

            return flowIssue;
        }

        private static async Task<List<CachedIssue>> RetrieveIssues()
        {
            using (var cacheRepo = new SqliteJiraLocalCacheRepository(@"../../../Data/issuesCache.db"))
            {
                await cacheRepo.Initialize();

                return (await cacheRepo.GetIssues()).ToList();
            }
        }
    }
}
