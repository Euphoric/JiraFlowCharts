﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jira.Querying;

namespace Jira.FlowCharts
{
    public interface ITasksSourceJiraCacheAdapter
    {
        Task<List<CachedIssue>> GetIssues();
        Task UpdateIssues(JiraLoginParameters jiraLoginParameters);
    }

    public class TasksSourceJiraCacheAdapter : ITasksSourceJiraCacheAdapter
    {
        private Func<JiraLocalCache.IRepository> _cacheRepositoryFactory;
        private readonly Func<JiraLoginParameters, IJiraClient> _clientFactory;

        public TasksSourceJiraCacheAdapter(
            Func<JiraLocalCache.IRepository> cacheRepositoryFactory,
            Func<JiraLoginParameters, IJiraClient> clientFactory)
        {
            _cacheRepositoryFactory = cacheRepositoryFactory;
            _clientFactory = clientFactory;
        }

        public async Task<List<CachedIssue>> GetIssues()
        {
            using (var cache = new JiraLocalCache(_cacheRepositoryFactory()))
            {
                await cache.Initialize();

                return (await cache.GetIssues()).ToList();
            }
        }

        public async Task UpdateIssues(JiraLoginParameters jiraLoginParameters)
        {
            using (var cache = new JiraLocalCache(_cacheRepositoryFactory()))
            {
                await cache.Initialize();

                var client = _clientFactory(jiraLoginParameters);

                await cache.Update(client, DateTime.MinValue);
            }
        }
    }
}