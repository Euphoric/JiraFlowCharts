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
            var dbContextOptions = CreateSqliteOptions(databaseFile);
            _dbContext = new IssuesCacheContext(dbContextOptions);

            var config = new MapperConfiguration(cfg => {
                cfg.CreateMap<CachedIssue, CachedIssueDb>();
                cfg.CreateMap<CachedIssueStatusChange, CachedIssueStatusChangeDb>();

                cfg.CreateMap<CachedIssueDb, CachedIssue>();
                cfg.CreateMap<CachedIssueStatusChangeDb, CachedIssueStatusChange>();
            }
);
            _mapper = config.CreateMapper();
        }

        internal static DbContextOptions<IssuesCacheContext> CreateSqliteOptions(string databaseFile)
        {
            var optionsBuilder = new DbContextOptionsBuilder<IssuesCacheContext>();
            var connectionString =  databaseFile == null ? "DataSource=:memory:" : $"DataSource={Path.GetFullPath(databaseFile)}";
            optionsBuilder.UseSqlite(connectionString);
            var optionsBuilderOptions = optionsBuilder.Options;
            return optionsBuilderOptions;
        }

        public async Task Initialize()
        {
            await _dbContext.Database.OpenConnectionAsync();
            await _dbContext.Database.MigrateAsync();
        }

        public async Task<ProjectStatistic[]> GetProjects()
        {
            var projectStatistics = 
                await _dbContext.Issues
                    .GroupBy(x=>x.Project)
                    .Select(grp=>new ProjectStatistic(grp.Key, grp.Count(), grp.Max(x=>x.Updated)))
                    .ToArrayAsync();

            return projectStatistics;
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

        public async Task<List<CachedIssue>> GetIssues(string projectKey = null)
        {
            IQueryable<CachedIssueDb> issues = _dbContext.Issues;
            if (projectKey != null)
            {
                issues = issues.Where(x => x.Key.StartsWith(projectKey));
            }
            var dbIssues = await issues.ToListAsync();

            return _mapper.Map<List<CachedIssue>>(dbIssues);
        }

        public async Task<DateTime?> LastUpdatedIssueTime(string projectKey)
        {
            return await _dbContext.Issues.Where(x=>x.Key.StartsWith(projectKey)).Select(x => x.Updated).MaxAsync();
        }

        public async Task<string[]> GetStatuses()
        {
            var issueStatuses = _dbContext.Issues.Select(x => x.Status);
            var issueChangeStatuses = _dbContext.Issues.SelectMany(x => x.StatusChanges).Select(x => x.State);
            var statuses = await issueStatuses.Union(issueChangeStatuses).ToArrayAsync();

            return statuses;
        }

        public void Dispose()
        {
            _dbContext.Dispose(); // WARN: Not unit tested. 
            IsDisposed = true;
        }
    }
}
