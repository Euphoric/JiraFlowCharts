using System;

namespace Jira.Querying
{
    public class CachedIssueStatusChange
    {
        public CachedIssueStatusChange(DateTime changeTime, string state)
        {
            ChangeTime = changeTime;
            State = state;
        }

        public DateTime ChangeTime { get; set; }

        public string State { get; set; }
    }
}
