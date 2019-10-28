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
        Task UpdateIssues(JiraLoginParameters jiraLoginParameters, ICacheUpdateProgress cacheUpdateProgress);
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

        public async Task UpdateIssues(JiraLoginParameters jiraLoginParameters, ICacheUpdateProgress cacheUpdateProgress)
        {
            using (var cache = new JiraLocalCache(CreateRepository()))
            {
                await cache.Initialize();

                var client = new JiraClient(jiraLoginParameters);

                await cache.Update(client, DateTime.MinValue, cacheUpdateProgress);
            }
        }
    }
}