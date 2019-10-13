using System;

namespace Jira.Querying.Sqlite
{
    public class CachedIssueStatusChangeDb
    {
        public int Id { get; set; }
        public string IssueKey { get; set; }
        public DateTime ChangeTime { get; set; }
        public string State { get; set; }
    }
}
