using Jira.Querying;
using System;
using System.Collections.ObjectModel;

namespace Jira.FlowCharts
{
    public class AnalyzedIssue
    {
        public string Project { get; set; }
        public string Key { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public string Resolution { get; set; }
        public string Status { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? Updated { get; set; }
        public DateTime? Resolved { get; set; }
        public int? OriginalEstimate { get; set; }
        public int? TimeSpent { get; set; }
        public int? StoryPoints { get; set; }
        public Collection<CachedIssueStatusChange> StatusChanges { get; set; }

        public Collection<CachedIssueStatusChange> SimplifiedStatusChanges { get; set; }
        public DateTime? Started { get; set; }
        public DateTime? Ended { get; set; }
        public TimeSpan? Duration { get; set; }
    }
}
