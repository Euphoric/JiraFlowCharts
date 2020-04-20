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
        Task<List<CachedIssue>> GetIssues(string projectKey);
        Task UpdateIssues(JiraLoginParameters jiraLoginParameters, string projectKey, ICacheUpdateProgress cacheUpdateProgress, DateTime startUpdateDate);
        Task<string[]> GetAllStates();
        Task<ProjectStatistic[]> GetProjects();
    }

    public class TasksSourceJiraCacheAdapter : ITasksSourceJiraCacheAdapter, IDisposable
    {
        private readonly JiraLocalCache _cache;

        public TasksSourceJiraCacheAdapter(SqliteJiraLocalCacheRepository sqliteJiraLocalCacheRepository)
        {
            _cache = new JiraLocalCache(sqliteJiraLocalCacheRepository);
        }

        public async Task<List<CachedIssue>> GetIssues(string projectKey)
        {
            return (await _cache.GetIssues(projectKey)).ToList();
        }

        public async Task UpdateIssues(JiraLoginParameters jiraLoginParameters, string projectKey, ICacheUpdateProgress cacheUpdateProgress, DateTime startUpdateDate)
        {
            var client = new JiraClient(jiraLoginParameters);

            await _cache.Update(client, startUpdateDate, projectKey, cacheUpdateProgress);
        }

        public async Task<string[]> GetAllStates()
        {
            return await _cache.GetStatuses();
        }

        public async Task<ProjectStatistic[]> GetProjects()
        {
            return await _cache.GetProjects();
        }

        public void Dispose()
        {
            _cache.Dispose();
        }
    }
}