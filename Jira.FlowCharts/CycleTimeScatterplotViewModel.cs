using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;

namespace Jira.FlowCharts
{
    public class CycleTimeScatterplotViewModel
    {
        public class IssuePoint : ObservablePoint
        {
            public string Label { get; }

            public IssuePoint(string label, double x, double y)
                :base(x, y)
            {
                Label = label;
            }
        }

        public ChartValues<IssuePoint> Stories { get; }
        public ChartValues<IssuePoint> Bugs { get;  }

        public double Percentile50 { get; }
        public double Percentile70 { get; }
        public double Percentile85 { get; }
        public double Percentile95 { get; }

        public Func<ChartPoint, string> LabelPoint { get; }

        public Func<double, string> Formatter { get; set; }

        DateTime baseDate = new DateTime(1980, 1, 1, 0, 0, 0);

        public CycleTimeScatterplotViewModel(FlowIssue[] flowIssues)
        {
            Stories = new ChartValues<IssuePoint>();
            Bugs = new ChartValues<IssuePoint>();

            flowIssues = flowIssues.Where(x => x.Start.HasValue && x.End.HasValue).ToArray();

            foreach (var issue in flowIssues)
            {
                var sinceStart = issue.End.Value - baseDate;
                var label = ""
                            + issue.Key + Environment.NewLine
                            + issue.Title + Environment.NewLine
                            + issue.End.Value.ToString(CultureInfo.InvariantCulture);

                var issuePoint = new IssuePoint(label, sinceStart.TotalDays, issue.Duration.Value);

                if (issue.Type == "Story")
                {
                    Stories.Add(issuePoint);
                }
                else if (issue.Type == "Bug")
                {
                    Bugs.Add(issuePoint);
                }
            }

            var durations = flowIssues.Select(x => x.Duration.Value).OrderBy(x => x).ToArray();

            Percentile50 = durations[(int)(durations.Length * 0.50)];
            Percentile70 = durations[(int)(durations.Length * 0.70)];
            Percentile85 = durations[(int)(durations.Length * 0.85)];
            Percentile95 = durations[(int)(durations.Length * 0.95)];

            LabelPoint = x => IssuePointLabel((IssuePoint) x.Instance);
            Formatter = x => (baseDate + TimeSpan.FromDays(x)).ToString("d/M/yy", CultureInfo.InvariantCulture);
        }

        private static string IssuePointLabel(IssuePoint issuePoint)
        {
            return issuePoint.Label;
        }
    }
}