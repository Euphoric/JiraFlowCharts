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

        public CumulativeFlowAnalysis(IEnumerable<FlatIssue> stories, string[] states)
        {
            // TODO : Follow correct order of states

            States = states.Reverse().ToArray();
            var stateIxs = States.Select((x, i) => new { i, x }).ToDictionary(x => x.x, x => x.i);

            List<ChangePoint> changes = new List<ChangePoint>();
            int[] statesCounter = new int[States.Length];
            var groupedStories = 
                stories
                .SelectMany(x => x.StatusChanges)
                .Where(st => stateIxs.ContainsKey(st.State))
                .GroupBy(x => x.ChangeTime.Date)
                .OrderBy(x=>x.Key);

            foreach (var grp in groupedStories)
            {
                foreach (var item in grp)
                {
                    if (!stateIxs.ContainsKey(item.State))
                        continue;

                    var stateIx = stateIxs[item.State];
                    statesCounter[stateIx]++;

                    if (stateIx < states.Length-1)
                        statesCounter[stateIx + 1]--;
                }

                var cp = new ChangePoint() { Date = grp.Key, StateCounts = statesCounter.ToArray() };

                changes.Add(cp);
            }

            Changes = changes.ToArray();
        }
    }
}