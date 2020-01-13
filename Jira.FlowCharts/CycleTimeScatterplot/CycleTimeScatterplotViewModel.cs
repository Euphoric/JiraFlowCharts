using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Caliburn.Micro;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Defaults;
using LiveCharts.Wpf;

namespace Jira.FlowCharts
{
    public class CycleTimeScatterplotViewModel : Screen
    {
        static CycleTimeScatterplotViewModel()
        {
            var mapper1 = Mappers.Xy<IssuePoint>()
                .X(value => value.X)
                .Y(value => value.Y);
            Charting.For<IssuePoint>(mapper1);
        }

        public class IssuePoint : ObservablePoint
        {
            public string Label { get; }

            public IssuePoint(string label, double x, double y)
                :base(x, y)
            {
                Label = label;
            }
        }

        readonly DateTime _baseDate = new DateTime(1980, 1, 1, 0, 0, 0);
        private readonly TasksSource _taskSource;
        private ChartValues<IssuePoint> _stories;
        private ChartValues<IssuePoint> _bugs;
        private Func<ChartPoint, string> _labelPoint;
        private Func<double, string> _formatter;
        private double _percentile50;
        private double _percentile70;
        private double _percentile85;
        private double _percentile95;
        private double _days14;
        private double _days7;

        public ChartValues<IssuePoint> Stories
        {
            get => _stories;
            private set => Set(ref _stories, value);
        }

        public ChartValues<IssuePoint> Bugs
        {
            get => _bugs;
            private set => Set(ref _bugs, value);
        }

        public Func<ChartPoint, string> LabelPoint
        {
            get => _labelPoint;
            private set => Set(ref _labelPoint, value);
        }

        public Func<double, string> Formatter
        {
            get => _formatter;
            private set => Set(ref _formatter, value);
        }

        public double Percentile50
        {
            get => _percentile50;
            private set => Set(ref _percentile50, value);
        }

        public double Percentile70
        {
            get => _percentile70;
            private set => Set(ref _percentile70, value);
        }

        public double Percentile85
        {
            get => _percentile85;
            private set => Set(ref _percentile85, value);
        }

        public double Percentile95
        {
            get => _percentile95;
            private set => Set(ref _percentile95, value);
        }

        public double Days7
        {
            get => _days7;
            private set => Set(ref _days7, value);
        }

        public double Days14
        {
            get => _days14;
            private set => Set(ref _days14, value);
        }

        public CycleTimeScatterplotViewModel(TasksSource taskSource)
        {
            _taskSource = taskSource;

            DisplayName = "Cycle time scatterplot";
        }

        protected override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            Stories = new ChartValues<IssuePoint>();
            Bugs = new ChartValues<IssuePoint>();

            var finishedTasks = await _taskSource.GetLatestFinishedStories();
            foreach (var issue in finishedTasks)
            {
                var sinceStart = issue.Ended - _baseDate;
                var label = ""
                            + issue.Key + Environment.NewLine
                            + issue.Title + Environment.NewLine
                            + issue.Ended.ToString(CultureInfo.InvariantCulture);

                var issuePoint = new IssuePoint(label, sinceStart.TotalDays, issue.DurationDays);

                if (issue.Type == "Story")
                {
                    Stories.Add(issuePoint);
                }
                else if (issue.Type == "Bug")
                {
                    Bugs.Add(issuePoint);
                }
            }

            var durations = finishedTasks.Select(x => x.DurationDays).OrderBy(x => x).ToArray();

            Percentile50 = durations[(int)(durations.Length * 0.50)];
            Percentile70 = durations[(int)(durations.Length * 0.70)];
            Percentile85 = durations[(int)(durations.Length * 0.85)];
            Percentile95 = durations[(int)(durations.Length * 0.95)];

            var firstDays7Index = durations.Select((x, i) => ValueTuple.Create(i, x)).First(t => t.Item2 > 7).Item1;
            Days7 = ((double)firstDays7Index / durations.Count()) * 100;

            var firstDays14Index = durations.Select((x, i) => ValueTuple.Create(i, x)).First(t => t.Item2 > 14).Item1;
            Days14 = ((double) firstDays14Index / durations.Count()) * 100;

            LabelPoint = x => IssuePointLabel((IssuePoint)x.Instance);
            Formatter = x => (_baseDate + TimeSpan.FromDays(x)).ToString("d/M/yy", CultureInfo.InvariantCulture);
        }

        private static string IssuePointLabel(IssuePoint issuePoint)
        {
            return issuePoint.Label;
        }
    }
}