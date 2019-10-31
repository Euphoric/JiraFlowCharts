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
        Task UpdateIssues(JiraLoginParameters jiraLoginParameters, string projectKey, ICacheUpdateProgress cacheUpdateProgress);
        Task<string[]> GetAllStates();
    }

    public class TasksSourceJiraCacheAdapter : ITasksSourceJiraCacheAdapter
    {
        private JiraLocalCache.IRepository CreateRepository()
        {
            return new SqliteJiraLocalCacheRepository(@"../../../Data/issuesCache.db");
        }

        public async Task<List<CachedIssue>> GetIssues()
        {
            using (var cache = new JiraLocalCache(CreateRepository()))
            {
                await cache.Initialize();

                return (await cache.GetIssues()).ToList();
            }
        }

        public async Task UpdateIssues(JiraLoginParameters jiraLoginParameters, string projectKey, ICacheUpdateProgress cacheUpdateProgress)
        {
            using (var cache = new JiraLocalCache(CreateRepository()))
            {
                await cache.Initialize();

                var client = new JiraClient(jiraLoginParameters);

                await cache.Update(client, DateTime.MinValue, projectKey, cacheUpdateProgress);
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