using Jira.Querying;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;

namespace Jira.FlowCharts
{

    public class CumulativeFlowViewModel : Screen
    {
        private readonly TasksSource _source;

        public CumulativeFlowViewModel(TasksSource source)
        {
            _source = source;
            DisplayName = "Cumulative flow";

            SeriesCollection = new SeriesCollection();
            XFormatter = val => new DateTime((long)val).ToShortDateString();
        }

        public SeriesCollection SeriesCollection { get; private set; }
        public Func<double, string> XFormatter { get; private set; }

        protected override async Task OnActivateAsync(CancellationToken token)
        {
            var fromDate = DateTime.Now.AddMonths(-3);

            var cfa = new CumulativeFlowAnalysis(await _source.GetAllTasks(), _source.States, fromDate);

            SeriesCollection.Clear();

            foreach (var state in cfa.States)
            {
                SeriesCollection.Add(new StackedAreaSeries
                {
                    Title = state,
                    Values = new ChartValues<DateTimePoint>(),
                    LineSmoothness = 0
                });
            }

            foreach (var change in cfa.Changes)
            {
                for (int i = 0; i < cfa.States.Length; i++)
                {
                    SeriesCollection[i].Values.Add(new DateTimePoint(change.Date, change.StateCounts[i]));
                }
            }
        }
    }
}