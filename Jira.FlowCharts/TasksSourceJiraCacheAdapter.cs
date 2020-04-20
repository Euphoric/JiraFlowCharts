﻿using System;
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

        public async Task<List<CachedIssue>> GetIssues(string projectKey)
        {
            using (var cache = new JiraLocalCache(CreateRepository()))
            {
                await cache.Initialize();

                return (await cache.GetIssues(projectKey)).ToList();
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

                return await cache.GetStatuses();
            }
        }

        public async Task<ProjectStatistic[]> GetProjects()
        {
            using (var cache = new JiraLocalCache(CreateRepository()))
            {
                await cache.Initialize();

                return await cache.GetProjects();
            }
        }
    }
}