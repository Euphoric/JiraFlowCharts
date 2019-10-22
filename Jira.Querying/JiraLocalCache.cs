using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Jira.Querying
{
    public class JiraLocalCache : IDisposable
    {
        public interface IRepository : IDisposable
        {
            Task Initialize();

            Task<IEnumerable<CachedIssue>> GetIssues();

            Task AddOrReplaceCachedIssue(CachedIssue flatIssue);

            Task<DateTime?> LastUpdatedIssueTime();

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

            public Task<IEnumerable<CachedIssue>> GetIssues()
            {
                return Task.FromResult<IEnumerable<CachedIssue>>(_issues);
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

            public Task<DateTime?> LastUpdatedIssueTime()
            {
                return Task.FromResult(_issues.Select(x => x.Updated).Max());
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

        private DateTime? _startUpdateDate;

        public JiraLocalCache(IRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<CachedIssue>> GetIssues()
        {
            return await _repository.GetIssues();
        }

        public async Task Initialize(DateTime startDateTime)
        {
            _startUpdateDate = startDateTime;

            await _repository.Initialize();
        }

        public async Task Update(IJiraClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (!_startUpdateDate.HasValue)
            {
                throw new InvalidOperationException($"Must call {nameof(Initialize)} before updating.");
            }

            string projectName = "AC"; // TODO : Parametrize project

            DateTime lastUpdateDate = await GetLastUpdateDateTime();

            int itemPaging = 0;
            while (true)
            {
                const int QueryLimit = 50;

                IJiraIssue[] updatedIssues = await client.GetIssues(projectName, lastUpdateDate, QueryLimit, itemPaging);

                foreach (var issue in updatedIssues)
                {
                    CachedIssue flatIssue = await client.RetrieveDetails(issue);

                    await _repository.AddOrReplaceCachedIssue(flatIssue);
                }

                itemPaging += QueryLimit;

                if (updatedIssues.Length != QueryLimit)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Retrieves last updated date. If there are no issues, uses set default. If there are, uses date time of last updated issue.
        /// </summary>
        private async Task<DateTime> GetLastUpdateDateTime()
        {
            DateTime lastUpdateDate = _startUpdateDate.Value;

            var lastIssueUpdate = await _repository.LastUpdatedIssueTime();

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
