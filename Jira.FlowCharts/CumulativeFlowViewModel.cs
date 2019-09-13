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

            var states = new[] { "Ready For Dev", "In Dev", "Ready for Peer Review", "Ready for QA", "In QA", "Ready for Done", "Done" };
            var currentStatus = stories.Select(x => x.Status).Distinct().ToArray();
            var currentResolution = stories.Select(x => x.Resolution).Distinct().ToArray();
            var allStates = stories.SelectMany(x => x.StatusChanges).Select(x => x.State).Distinct().ToArray();

            var fromDate = DateTime.Now.AddMonths(-3);

            var cfa = new CumulativeFlowAnalysis(stories, states, fromDate);

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

            XFormatter = val => new DateTime((long)val).ToShortDateString();
        }

        public SeriesCollection SeriesCollection { get; private set; }
        public Func<double, string> XFormatter { get; private set; }
    }
}