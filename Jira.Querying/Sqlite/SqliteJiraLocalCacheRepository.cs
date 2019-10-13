using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Jira.Querying.Sqlite
{
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
                Title = issue.Title,
                Type = issue.Type,
                Status = issue.Status,
                TimeSpent = issue.TimeSpent,
                OriginalEstimate = issue.OriginalEstimate,
                Resolution = issue.Resolution,
                Resolved = issue.Resolved,
                StoryPoints = issue.StoryPoints
            };

            _dbContext.Issues.Add(issueDb);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<IEnumerable<CachedIssue>> GetIssues()
        {
            var dbIssues = await _dbContext.Issues.ToArrayAsync();
            var issues = dbIssues.Select(issue => new CachedIssue()
            {
                Key = issue.Key,
                Created = issue.Created,
                Updated = issue.Updated,
                Title = issue.Title,
                Type = issue.Type,
                Status = issue.Status,
                TimeSpent = issue.TimeSpent,
                OriginalEstimate = issue.OriginalEstimate,
                Resolution = issue.Resolution,
                Resolved = issue.Resolved,
                StoryPoints = issue.StoryPoints
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
