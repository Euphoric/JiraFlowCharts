using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jira.Querying;
using Jira.Querying.Sqlite;

namespace Jira.FlowCharts
{
    public interface ITasksSourceJiraCacheAdapter
    {
        Task<List<CachedIssue>> GetIssues();
        Task UpdateIssues(JiraLoginParameters jiraLoginParameters, string projectKey, ICacheUpdateProgress cacheUpdateProgress, DateTime startUpdateDate);
        Task<string[]> GetAllStates();
    }

    public class TasksSourceJiraCacheAdapter : ITasksSourceJiraCacheAdapter
    {
        private readonly string _databaseFile;

        public TasksSourceJiraCacheAdapter(string databaseFile)
        {
            _databaseFile = databaseFile;
        }

        private JiraLocalCache.IRepository CreateRepository()
        {
            return new SqliteJiraLocalCacheRepository(_databaseFile);
        }

        public async Task<List<CachedIssue>> GetIssues()
        {
            using (var cache = new JiraLocalCache(CreateRepository()))
            {
                await cache.Initialize();

                return (await cache.GetIssues()).ToList();
            }
        }

        public async Task UpdateIssues(JiraLoginParameters jiraLoginParameters, string projectKey, ICacheUpdateProgress cacheUpdateProgress, DateTime startUpdateDate)
        {
            using (var cache = new JiraLocalCache(CreateRepository()))
            {
                await cache.Initialize();

                var client = new JiraClient(jiraLoginParameters);

                await cache.Update(client, startUpdateDate, projectKey, cacheUpdateProgress);
            }
        }

        public async Task<string[]> GetAllStates()
        {
            using (var cache = new JiraLocalCache(CreateRepository()))
            {
                await cache.Initialize();

                var cachedIssues = await cache.GetIssues();
                var issueStatus = cachedIssues.Select(x=>x.Status);
                var issueChangeStatus = cachedIssues.SelectMany(x => x.StatusChanges).Select(x => x.State);

                return issueStatus.Concat(issueChangeStatus).Distinct().ToArray();
            }
        }
    }
}