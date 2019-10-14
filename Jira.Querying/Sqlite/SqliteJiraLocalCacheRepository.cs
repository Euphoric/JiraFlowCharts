using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Jira.Querying.Sqlite
{
    public class SqliteJiraLocalCacheRepository : JiraLocalCache.IRepository
    {
        readonly IssuesCacheContext _dbContext;
        readonly IMapper _mapper;

        public bool IsDisposed { get; private set; }

        public SqliteJiraLocalCacheRepository(string databaseFile = null)
        {
            var optionsBuilder = new DbContextOptionsBuilder<IssuesCacheContext>();
            var connectionString = databaseFile == null ? "DataSource=:memory:" : $"DataSource={Path.GetFullPath(databaseFile)}";
            optionsBuilder.UseSqlite(connectionString);
            _dbContext = new IssuesCacheContext(optionsBuilder.Options);

            var config = new MapperConfiguration(cfg => {
                cfg.CreateMap<CachedIssue, CachedIssueDb>();
                cfg.CreateMap<CachedIssueStatusChange, CachedIssueStatusChangeDb>();

                cfg.CreateMap<CachedIssueDb, CachedIssue>();
                cfg.CreateMap<CachedIssueStatusChangeDb, CachedIssueStatusChange>();
            }
);
            _mapper = config.CreateMapper();
        }

        public async Task Initialize()
        {
            await _dbContext.Database.OpenConnectionAsync();
            await _dbContext.Database.MigrateAsync();
        }

        public async Task AddOrReplaceCachedIssue(CachedIssue issue)
        {
            var existingIssue = await _dbContext.Issues.FirstOrDefaultAsync(x => x.Key == issue.Key);
            if (existingIssue != null)
            {
                _dbContext.Issues.Remove(existingIssue);
            }

            await _dbContext.SaveChangesAsync();

            CachedIssueDb issueDb = _mapper.Map<CachedIssueDb>(issue);

            _dbContext.Issues.Add(issueDb);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<IEnumerable<CachedIssue>> GetIssues()
        {
            var dbIssues = await _dbContext.Issues.ToArrayAsync();

            return _mapper.Map<IEnumerable<CachedIssue>>(dbIssues);
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
