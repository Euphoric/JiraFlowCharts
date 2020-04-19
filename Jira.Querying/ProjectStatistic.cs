using System;

namespace Jira.Querying
{
    public class ProjectStatistic
    {
        public string Key { get; }
        public int IssueCount { get; }
        public DateTime? LastUpdatedDate { get; }

        public ProjectStatistic(string key, int issueCount, DateTime? lastUpdatedDate)
        {
            Key = key;
            IssueCount = issueCount;
            LastUpdatedDate = lastUpdatedDate;
        }
    }
}