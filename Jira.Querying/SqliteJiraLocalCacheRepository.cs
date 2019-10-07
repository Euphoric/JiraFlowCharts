using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

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
        IssuesCacheContext _dbContext;

        public bool IsDisposed { get; private set; }

        public SqliteJiraLocalCacheRepository()
        {
            _dbContext = new IssuesCacheContext();
        }

        public async Task Initialize()
        {
            await _dbContext.Database.OpenConnectionAsync(); // needed for in-memory test (TODO : fix?)
            await _dbContext.Database.MigrateAsync();
        }

        public async Task AddOrReplaceCachedIssue(CachedIssue issue)
        {
            var existingIssue = await _dbContext.Issues.FirstOrDefaultAsync(x => x.Key == issue.Key);
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
            await _dbContext.SaveChangesAsync();
        }

        public async Task<IEnumerable<CachedIssue>> GetIssues()
        {
            var dbIssues = await _dbContext.Issues.ToArrayAsync();
            var issues = dbIssues.Select(iss => new CachedIssue()
            {
                Key = iss.Key,
                Created = iss.Created,
                Updated = iss.Updated
            });
            return issues;
        }

        public async Task<DateTime?> LastUpdatedIssueTime()
        {
            return await _dbContext.Issues.Select(x => x.Updated).MaxAsync();
        }

        public void Dispose()
        {
            _dbContext.Dispose(); // WARN: Not unit tested. 
            IsDisposed = true;
        }
    }
}
