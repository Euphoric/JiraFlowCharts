using Jira.Querying;
using System.Collections.Generic;
using System.Linq;

namespace Jira.FlowCharts
{
    public class SimplifyStateChangeOrder
    {
        private readonly Dictionary<string, int> _states;
        private readonly HashSet<string> _resetStates;

        // TODO : Last state should be taken from last occurence of change
        // TODO : State changes in same day/time are in right order

        public SimplifyStateChangeOrder(string[] states, string[] resetStates = null)
        {
            _states = states.Select((state, i) => new { state, i }).ToDictionary(x => x.state, x => x.i);
            _resetStates = new HashSet<string>(resetStates ?? new string[0]);
        }

        public IEnumerable<CachedIssueStatusChange> FilterStatusChanges(IEnumerable<CachedIssueStatusChange> statusChanges)
        {
            return SimplifyStateOrder(SkipAfterResetState(statusChanges));
        }

        private IEnumerable<CachedIssueStatusChange> SkipAfterResetState(IEnumerable<CachedIssueStatusChange> statusChanges)
        {
            var list = statusChanges.ToList();
            var resetStateIdx = list.FindLastIndex(x => _resetStates.Contains(x.State));
            if (resetStateIdx != -1)
            {
                return list.Skip(resetStateIdx + 1);
            }
            else
            {
                return statusChanges;
            }
        }

        public IEnumerable<CachedIssueStatusChange> SimplifyStateOrder(IEnumerable<CachedIssueStatusChange> statusChanges)
        {
            int previousStateIndex = -1;
            foreach (var change in statusChanges)
            {
                int stateIndex;
                if (!_states.TryGetValue(change.State, out stateIndex))
                {
                    continue;
                }

                if (previousStateIndex >= stateIndex)
                {
                    continue;
                }

                previousStateIndex = stateIndex;
                yield return change;
            }
        }
    }
}