using System;

namespace Jira.FlowCharts
{
    public class FlowIssue
    {
        public string Key { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }

        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }

        public double? Duration { get; set; }
        public int? StoryPoints { get; set; }
        public double? TimeSpent { get; set; }
    }
}