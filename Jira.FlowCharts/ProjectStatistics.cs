using System;

namespace Jira.FlowCharts
{
    public class ProjectStatistics
    {
        public string Key { get; }
        public int IssueCount { get; }
        public DateTime? LastUpdateTime { get; }

        public ProjectStatistics(string key, int issueCount, DateTime? lastUpdateTime)
        {
            this.Key = key;
            this.IssueCount = issueCount;
            this.LastUpdateTime = lastUpdateTime;
        }
    }
}