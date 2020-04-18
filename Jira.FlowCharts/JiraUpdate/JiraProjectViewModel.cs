using System;

namespace Jira.FlowCharts.JiraUpdate
{
    public class JiraProjectViewModel
    {
        public JiraProjectViewModel(string key, int cachedIssuesCount, DateTime? lastUpdatedIssue)
        {
            Key = key;
            CachedIssuesCount = cachedIssuesCount;
            LastUpdatedIssue = lastUpdatedIssue;
        }

        public string Key { get; }
        public int CachedIssuesCount { get; }
        public DateTime? LastUpdatedIssue { get; }
    }
}