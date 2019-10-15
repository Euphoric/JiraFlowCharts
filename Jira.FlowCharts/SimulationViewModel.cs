﻿using LiveCharts;
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

            var simStats = Simulation.FlowSimulationStatistics.RunSimulationStatistic(StoryCreationRate);

            SeriesCollection = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Story count",
                    Values = new ChartValues<double>(simStats.HistogramValues.Select(x=>(double)x))
                }
            };

            Labels = simStats.HistogramLabels.Select(x => x.ToString("F1")).ToArray();
        }
    }
}
