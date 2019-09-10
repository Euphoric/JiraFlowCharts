using System;
using System.Collections.ObjectModel;

namespace Jira.Querying
{
    public class FlatIssue
    {
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

        public Collection<FlatIssueStatusChange> StatusChanges { get; set; }
        public int? StoryPoints { get; set; }
    }
}
