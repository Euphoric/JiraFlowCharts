using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Jira.Querying.Sqlite
{
    public class CachedIssueDb
    {
        [Key]
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
        public ICollection<CachedIssueStatusChangeDb> StatusChanges { get; set; }
    }
}
