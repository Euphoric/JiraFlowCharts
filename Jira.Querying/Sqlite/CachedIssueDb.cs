using System;
using System.ComponentModel.DataAnnotations;

namespace Jira.Querying.Sqlite
{
    public class CachedIssueDb
    {
        [Key]
        public string Key { get; set; }
        // TODO : Remaining parameters, needs tests
        public DateTime? Created { get; set; }
        public DateTime? Updated { get; set; }
    }
}
