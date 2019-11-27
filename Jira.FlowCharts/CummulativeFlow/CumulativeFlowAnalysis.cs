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

        public CumulativeFlowAnalysis(IEnumerable<AnalyzedIssue> stories, string[] states, DateTime? from = null)
        {
            States = states.Reverse().ToArray();
            var stateIxs = States.Select((x, i) => new { i, x }).ToDictionary(x => x.x, x => x.i);

            SimplifyStateChangeOrder simplifyState = new SimplifyStateChangeOrder(states);

            List<ChangePoint> changes = new List<ChangePoint>();
            int[] statesCounter = new int[States.Length];
            var statusChangesGroups =
                stories
                .SelectMany(issue =>
                    simplifyState.FilterStatusChanges(issue.StatusChanges)
                    .Select(statusChange => new { IssueKey = issue.Key, statusChange.ChangeTime, statusChange.State })
                    )
                .Where(x => stateIxs.ContainsKey(x.State))
                .GroupBy(x => x.ChangeTime.Date)
                .OrderBy(x => x.Key);

            Dictionary<string, int> activeStates = new Dictionary<string, int>();

            foreach (var statusChangeGroup in statusChangesGroups)
            {
                foreach (var statusChange in statusChangeGroup)
                {
                    var currentStateIx = stateIxs[statusChange.State];
                    statesCounter[currentStateIx]++;

                    if (activeStates.TryGetValue(statusChange.IssueKey, out int previousStateIx))
                    {
                        statesCounter[previousStateIx]--;
                    }

                    activeStates[statusChange.IssueKey] = stateIxs[statusChange.State];
                }

                var cp = new ChangePoint() { Date = statusChangeGroup.Key, StateCounts = statesCounter.ToArray() };

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
    }
}