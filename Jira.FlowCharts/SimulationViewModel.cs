using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using System.Linq;

namespace Jira.FlowCharts
{
    public class SimulationViewModel
    {
        private FlowIssue[] finishedStories;

        public double StoryCreationRate { get; private set; }

        public SeriesCollection SeriesCollection { get; private set; }

        public string[] Labels { get; private set; }

        public double Percentile50 { get; private set; }
        public double Percentile75 { get; private set; }
        public double Percentile85 { get; private set; }
        public double Percentile95 { get; private set; }

        public SimulationViewModel(FlowIssue[] finishedStories)
        {
            this.finishedStories = finishedStories;

            var startTime = finishedStories.Max(x => x.End).AddMonths(-6);

            var stories =
                finishedStories.Where(x => x.Start > startTime)
                .ToArray();

            var from = stories.Min(x => x.Start);
            var to = stories.Max(x => x.End);

            StoryCreationRate = stories.Count() / (to - from).TotalDays;
            var cycleTimes = stories.Select(x => x.Duration).ToArray();

            var simStats = Simulation.FlowSimulationStatistics.RunSimulationStatistic(StoryCreationRate, cycleTimes, 10000, 10);

            SeriesCollection = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Final time count",
                    Values = new ChartValues<double>(simStats.HistogramValues.Select(x=>(double)x))
                }
            };

            Labels = simStats.HistogramLabels.Select(x => x.ToString("F1")).ToArray();

            Percentile50 = simStats.percentile50;
            Percentile75 = simStats.percentile75;
            Percentile85 = simStats.percentile85;
            Percentile95 = simStats.percentile95;

        }
    }
}
