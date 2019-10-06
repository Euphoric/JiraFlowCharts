using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Jira.Querying
{

    public class CachedIssueDb
    {
        [Key]
        public string Key { get; set; }
        // TODO : Remaining parameters, needs tests
        public DateTime? Created { get; set; }
        public DateTime? Updated { get; set; }
    }

    class IssuesCacheContext : DbContext
    {
        public DbSet<CachedIssueDb> Issues { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("DataSource=:memory:"); // TODO: use connection string from constructor

            base.OnConfiguring(optionsBuilder);
        }
    }

    public class SqliteJiraLocalCacheRepository : JiraLocalCache.IRepository
    {
        IssuesCacheContext _dbContext; // TODO: dispose after usage

        public SqliteJiraLocalCacheRepository()
        {
            _dbContext = new IssuesCacheContext();
            _dbContext.Database.OpenConnection(); // needed for in-memory test (TODO : fix?)
            _dbContext.Database.Migrate();
        }

        public void AddOrReplaceCachedIssue(CachedIssue issue)
        {
            // TODO : Async in all operations
            var existingIssue = _dbContext.Issues.FirstOrDefault(x => x.Key == issue.Key);
            if (existingIssue != null)
            {
                _dbContext.Issues.Remove(existingIssue);
            }

            var issueDb = new CachedIssueDb()
            {
                Key = issue.Key,
                Created = issue.Created,
                Updated = issue.Updated,
            };

            _dbContext.Issues.Add(issueDb);
            _dbContext.SaveChanges();
        }

        public Collection<CachedIssue> GetIssues()
        {
            var dbIssues = _dbContext.Issues.ToArray();
            var issues = dbIssues.Select(iss => new CachedIssue()
            {
                Key = iss.Key,
                Created = iss.Created,
                Updated = iss.Updated
            });
            return new Collection<CachedIssue>(issues.ToList());
        }

        public DateTime? LastUpdatedIssueTime()
        {
            return _dbContext.Issues.Select(x => x.Updated).Max();
        }
    }
}
