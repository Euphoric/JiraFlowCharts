using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Jira.FlowCharts
{

    public class CumulativeFlowViewModel
    {
        public CumulativeFlowViewModel(IEnumerable<FlatIssue> stories)
        {
            stories = stories.Where(x => x.Status != "Withdrawn" && x.Status != "On Hold" && x.Resolution != "Duplicate");

            var states = new[] { "In Dev", "In QA", "Done" };
            var currentStatus = stories.Select(x => x.Status).Distinct().ToArray();
            var currentResolution = stories.Select(x => x.Resolution).Distinct().ToArray();
            var allStates = stories.SelectMany(x => x.StatusChanges).Select(x => x.State).Distinct().ToArray();

            var cfa = new CumulativeFlowAnalysis(stories, states);

            var qqq = cfa.Changes;

            SeriesCollection = new SeriesCollection();

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

            XFormatter = val => new DateTime((long)val).ToString("yyyy");
            YFormatter = val => val.ToString("N") + " M";
        }

        public SeriesCollection SeriesCollection { get; private set; }
        public Func<double, string> XFormatter { get; private set; }
        public Func<double, string> YFormatter { get; private set; }
    }
}