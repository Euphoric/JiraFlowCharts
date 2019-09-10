using System;

namespace Jira.Querying
{
    public class FlatIssueStatusChange
    {
        public FlatIssueStatusChange(DateTime changeTime, string state)
        {
            ChangeTime = changeTime;
            State = state;
        }

        public DateTime ChangeTime { get; set; }

        public string State { get; set; }
    }
}
