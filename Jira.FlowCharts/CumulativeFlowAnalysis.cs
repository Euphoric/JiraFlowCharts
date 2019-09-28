using Jira.Querying;
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

        private IEnumerable<StateRange> ChangeRanges(Collection<CachedIssueStatusChange> statusChanges)
        {
            List<StateRange> stateRanges = new List<StateRange>();

            return stateRanges;
        }

        public CumulativeFlowAnalysis(IEnumerable<CachedIssue> stories, string[] states, DateTime? from = null)
        {
            // TODO : Follow correct order of states
            // TODO : Last state should be taken from last occurence of change
            // TODO : States going back to state that wasn't yet visited is ignored
            // TODO : State changes in same day/time are in right order

            States = states.Reverse().ToArray();
            var stateIxs = States.Select((x, i) => new { i, x }).ToDictionary(x => x.x, x => x.i);

            List<ChangePoint> changes = new List<ChangePoint>();
            int[] statesCounter = new int[States.Length];
            var groupedStories =
                stories
                .SelectMany(issue =>
                    FilterStatusChanges(issue.StatusChanges)
                    .Select(statusChange => new { IssueKey = issue.Key, statusChange.ChangeTime, statusChange.State })
                    )
                .Where(x => stateIxs.ContainsKey(x.State))
                .GroupBy(x => x.ChangeTime.Date)
                .OrderBy(x => x.Key);

            Dictionary<string, int> activeStates = new Dictionary<string, int>();

            foreach (var grp in groupedStories)
            {
                foreach (var change in grp)
                {
                    var currentStateIx = stateIxs[change.State];
                    statesCounter[currentStateIx]++;

                    if (activeStates.TryGetValue(change.IssueKey, out int previousStateIx))
                    {
                        statesCounter[previousStateIx]--;
                    }

                    activeStates[change.IssueKey] = stateIxs[change.State];
                }

                var cp = new ChangePoint() { Date = grp.Key, StateCounts = statesCounter.ToArray() };

                changes.Add(cp);
            }

            IEnumerable<ChangePoint> changesFiltered = changes;

            if (from != null)
            {
                changesFiltered = FilterChanges(changesFiltered, from.Value);
            }

            Changes = changesFiltered.ToArray();
        }

        private static IEnumerable<ChangePoint> FilterChanges(IEnumerable<ChangePoint> changesFiltered, DateTime fromValue)
        {
            ChangePoint previousChange = null;
            int? baseDoneStateCount = null;
            foreach (var change in changesFiltered)
            {
                if (change.Date >= fromValue)
                {
                    if (baseDoneStateCount == null)
                    {
                        if (previousChange == null)
                        {
                            baseDoneStateCount = 0;
                        }
                        else
                        {
                            baseDoneStateCount = previousChange.StateCounts[0];
                        }
                    }

                    change.StateCounts[0] -= baseDoneStateCount.Value;

                    yield return change;
                }

                previousChange = change;
            }
        }

        private IEnumerable<CachedIssueStatusChange> FilterStatusChanges(IEnumerable<CachedIssueStatusChange> statusChanges)
        {
            HashSet<string> foundStates = new HashSet<string>();
            foreach (var change in statusChanges)
            {
                if (foundStates.Add(change.State))
                {
                    yield return change;
                }
            }
        }
    }
}