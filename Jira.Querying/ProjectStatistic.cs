using System;

namespace Jira.Querying
{
    public class ProjectStatistic
    {
        public string Key { get; }
        public int IssueCount { get; }
        public DateTime? LastUpdatedTime { get; }

        public ProjectStatistic(string key, int issueCount, DateTime? lastUpdatedTime)
        {
            Key = key;
            IssueCount = issueCount;
            LastUpdatedTime = lastUpdatedTime;
        }
    }
}