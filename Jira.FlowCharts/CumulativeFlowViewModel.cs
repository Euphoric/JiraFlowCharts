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
        private readonly IEnumerable<CachedIssue> _stories;
        private readonly string[] _states;

        public CumulativeFlowViewModel(IEnumerable<CachedIssue> stories, string[] states)
        {
            _states = states;
            _stories = stories;

            DisplayName = "Cumulative flow";

            SeriesCollection = new SeriesCollection();
            XFormatter = val => new DateTime((long)val).ToShortDateString();
        }

        public SeriesCollection SeriesCollection { get; private set; }
        public Func<double, string> XFormatter { get; private set; }

        protected override Task OnActivateAsync(CancellationToken token)
        {
            var fromDate = DateTime.Now.AddMonths(-3);

            var cfa = new CumulativeFlowAnalysis(_stories, _states, fromDate);

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

            return Task.CompletedTask;
        }
    }
}