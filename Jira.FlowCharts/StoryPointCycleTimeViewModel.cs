using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Caliburn.Micro;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;

namespace Jira.FlowCharts
{
    public class StoryPointCycleTimeViewModel : Screen
    {
        private readonly FlowIssue[] _flowTasks;
        private string[] _labels;
        private SeriesCollection _seriesCollection;

        public SeriesCollection SeriesCollection
        {
            get => _seriesCollection;
            private set => Set(ref _seriesCollection, value);
        }

        public string[] Labels
        {
            get => _labels;
            set => Set(ref _labels, value);
        }

        public StoryPointCycleTimeViewModel(FlowIssue[] flowTasks)
        {
            _flowTasks = flowTasks;
            DisplayName = "Story point vs. cycle time";
        }

        protected override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            var storyPointGrouped = _flowTasks
                .Where(x => x.IsDone)
                .Where(x => x.StoryPoints.HasValue)
                .Where(x => x.StoryPoints.Value > 0)
                .GroupBy(x => x.StoryPoints.Value)
                .Select(grp => new
                {
                    StoryPoints = grp.Key,
                    Min = grp.Min(x => x.Duration),
                    Percentile05 = Percentile(grp.Select(x => x.Duration), 0.05),
                    Percentile25 = Percentile(grp.Select(x => x.Duration), 0.25),
                    Percentile75 = Percentile(grp.Select(x => x.Duration), 0.75),
                    Percentile95 = Percentile(grp.Select(x => x.Duration), 0.95),
                    Max = grp.Max(x => x.Duration),
                    Average = grp.Average(x => x.Duration),
                    Median = Percentile(grp.Select(x => x.Duration), 0.5),
                    Count = grp.Count()
                })
                .OrderBy(x => x.StoryPoints)
                .ToArray();


            SeriesCollection = new SeriesCollection
            {
                new OhlcSeries()
                {
                    Values =
                        new ChartValues<OhlcPoint>(
                            storyPointGrouped
                                .Select(grp => new OhlcPoint(grp.Percentile25, grp.Max, grp.Min, grp.Percentile75))
                                .ToArray()),
                    Title = "Cycle times"
                },
                new LineSeries
                {
                    Values = new ChartValues<double>(storyPointGrouped.Select(x=>(double)x.Average)),
                    Fill = Brushes.Transparent,
                    Title = "Average cycle time"
                }
                ,
                new LineSeries
                {
                    Values = new ChartValues<double>(storyPointGrouped.Select(x=>(double)x.Count)),
                    Fill = Brushes.Transparent,
                    Title = "Issue count"
                }
            };

            Labels = storyPointGrouped.Select(x => x.StoryPoints.ToString()).ToArray();

            return base.OnActivateAsync(cancellationToken);
        }

        private double Percentile(IEnumerable<double> values, double percentile)
        {
            var ordered = values.OrderBy(x => x).ToArray();
            return ordered[(int)(ordered.Length * percentile)];
        }
    }
}