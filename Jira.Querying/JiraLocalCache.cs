using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Jira.Querying
{
    public interface ICacheUpdateProgress
    {
        void UpdatedIssue(string key, DateTime updated);
    }

    public class NullCacheUpdateProgres : ICacheUpdateProgress
    {
        public void UpdatedIssue(string key, DateTime updated)
        {
            // NULL operation
        }
    }

    public class JiraLocalCache : IDisposable
    {
        public interface IRepository : IDisposable
        {
            Task Initialize();

            Task<IEnumerable<CachedIssue>> GetIssues(string projectKey = null);

            Task AddOrReplaceCachedIssue(CachedIssue flatIssue);

            Task<DateTime?> LastUpdatedIssueTime(string projectKey);

            bool IsDisposed { get; }
        }

        private class InMemoryRepository : IRepository
        {
            readonly Collection<CachedIssue> _issues = new Collection<CachedIssue>();

            public bool IsDisposed { get; private set; }

            public Task Initialize()
            {
                return Task.CompletedTask;
            }

            public Task<IEnumerable<CachedIssue>> GetIssues(string projectKey = null)
            {
                IEnumerable<CachedIssue> issues = _issues;
                if (projectKey != null)
                {
                    issues = issues.Where(x => x.Key.StartsWith(projectKey));
                }
                return Task.FromResult(issues);
            }
            
            public Task AddOrReplaceCachedIssue(CachedIssue flatIssue)
            {
                var cachedIssue = _issues.FirstOrDefault(x => x.Key == flatIssue.Key);
                if (cachedIssue != null)
                {
                    _issues.Remove(cachedIssue);
                }

                _issues.Insert(Math.Max(0, _issues.Count - 2), flatIssue); // inserting things out-of-order, to simulate sql's behavior of not keeping order

                return Task.CompletedTask;
            }

            public Task<DateTime?> LastUpdatedIssueTime(string projectKey)
            {
                var lastUpdatedTime = 
                    _issues
                        .Where(x=>x.Key.StartsWith(projectKey))
                        .Select(x => x.Updated)
                        .Max();

                return Task.FromResult(lastUpdatedTime);
            }

            public void Dispose()
            {
                IsDisposed = true;
            }
        }

        public static IRepository CreateMemoryRepository()
        {
            return new InMemoryRepository();
        }

        private readonly IRepository _repository;

        private bool _isInitialized;

        public JiraLocalCache(IRepository repository)
        {
            _repository = repository;
        }

        [Obsolete("Use version with project key.")]
        public async Task<IEnumerable<CachedIssue>> GetIssues()
        {
            return await _repository.GetIssues();
        }

        public async Task<IEnumerable<CachedIssue>> GetIssues(string projectKey)
        {
            return await _repository.GetIssues(projectKey);
        }

        public async Task Initialize()
        {
            await _repository.Initialize();

            _isInitialized = true;
        }

        public async Task<IEnumerable<string>> GetProjects()
        {
            var issues = await GetIssues();
            return issues.Select(x => x.Key.Split('-')[0]).Distinct();
        }

        public async Task Update(IJiraClient client, DateTime startUpdateDate, string projectKey, ICacheUpdateProgress progress = null)
        {
            progress = progress ?? new NullCacheUpdateProgres();

            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (string.IsNullOrWhiteSpace(projectKey))
            {
                throw new ArgumentNullException(nameof(projectKey));
            }

            if (!_isInitialized)
            {
                throw new InvalidOperationException($"Must call {nameof(Initialize)} before updating.");
            }

            DateTime lastUpdateDate = await GetLastUpdateDateTime(projectKey, startUpdateDate);

            int itemPaging = 0;
            while (true)
            {
                const int queryLimit = 50;

                IJiraIssue[] updatedIssues = await client.GetIssues(projectKey, lastUpdateDate, queryLimit, itemPaging);

                foreach (var issue in updatedIssues)
                {
                    CachedIssue flatIssue = await client.RetrieveDetails(issue);

                    await _repository.AddOrReplaceCachedIssue(flatIssue);
                    progress.UpdatedIssue(flatIssue.Key, flatIssue.Updated.Value);
                }

                itemPaging += queryLimit;

                if (updatedIssues.Length != queryLimit)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Retrieves last updated date. If there are no issues, uses set default. If there are, uses date time of last updated issue.
        /// </summary>
        private async Task<DateTime> GetLastUpdateDateTime(string projectKey, DateTime startUpdatedDate)
        {
            DateTime lastUpdateDate = startUpdatedDate;

            var lastIssueUpdate = await _repository.LastUpdatedIssueTime(projectKey);

            if (lastIssueUpdate.HasValue)
            {
                lastUpdateDate = lastIssueUpdate.Value;
            }

            return lastUpdateDate;
        }

        public void Dispose()
        {
            _repository.Dispose();
        }
    }
}
