using Jira.Querying;
using System.Collections.Generic;

namespace Jira.FlowCharts
{
    public class SimplifyStateChangeOrder
    {
        // TODO : Follow correct order of states
        // TODO : Last state should be taken from last occurence of change
        // TODO : States going back to state that wasn't yet visited is ignored
        // TODO : State changes in same day/time are in right order

        public IEnumerable<CachedIssueStatusChange> FilterStatusChanges(IEnumerable<CachedIssueStatusChange> statusChanges)
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