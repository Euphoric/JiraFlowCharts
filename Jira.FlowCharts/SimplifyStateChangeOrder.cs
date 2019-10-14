using Jira.Querying;
using System.Collections.Generic;
using System.Linq;

namespace Jira.FlowCharts
{
    public class SimplifyStateChangeOrder
    {
        private readonly Dictionary<string, int> _states;

        // TODO : Follow correct order of states
        // TODO : Last state should be taken from last occurence of change
        // TODO : States going back to state that wasn't yet visited is ignored
        // TODO : State changes in same day/time are in right order

        public SimplifyStateChangeOrder(string[] states)
        {
            _states = states.Select((state, i) => new { state, i }).ToDictionary(x => x.state, x => x.i);
        }

        public IEnumerable<CachedIssueStatusChange> FilterStatusChanges(IEnumerable<CachedIssueStatusChange> statusChanges)
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