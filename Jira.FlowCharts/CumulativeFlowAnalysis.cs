using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Jira.FlowCharts
{
    public class CumulativeFlowAnalysis
    {
        public ChangePoint[] Changes { get; private set; }
        public string[] States { get; set; }

        public class ChangePoint
        {
            public DateTime Date { get; set; }
            public int[] StateCounts { get; set; }
        }

        private class StateRange
        {
            DateTime From { get; }
            DateTime To { get; }

            public StateRange(DateTime from, DateTime to)
            {
                if (from > to)
                    throw new ArgumentException("[from] must be less or equal to [to]");

                From = from;
                To = to;
            }
        }

        private IEnumerable<StateRange> ChangeRanges(Collection<FlatIssueStatusChange> statusChanges)
        {
            List<StateRange> stateRanges = new List<StateRange>();

            return stateRanges;
        }

        public CumulativeFlowAnalysis(IEnumerable<FlatIssue> stories)
        {
            var mergedChanges =
                stories.SelectMany(st => ChangeRanges(st.StatusChanges).Select(sc => new { story = st, change = sc }))
                .ToArray();

            //DateTime to = mergedChanges.Max(x => x.change.ChangeTime);
            //DateTime from = to.AddYears(-1);

            //DateTime current = from;

            //List<ChangePoint> changePoints = new List<ChangePoint>();

            //for (; current <= to; current += TimeSpan.FromDays(1))
            //{
            //    var changePoint = new ChangePoint();
            //    changePoint.Time = current;


            //    changePoints.Add(changePoint);
            //}

            States = stories.SelectMany(x => x.StatusChanges).Select(x => x.State).Distinct().ToArray();

            Changes = stories.SelectMany(x => x.StatusChanges).Select((q, i) => new ChangePoint() { Date = q.ChangeTime, StateCounts = new int[] { i + 1 } }).ToArray();
        }
    }
}