using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Jira.Querying
{
    public class JiraLocalCache
    {
        public interface IRepository
        {
            Collection<CachedIssue> GetIssues();

            void AddOrReplaceCachedIssue(CachedIssue flatIssue);

            DateTime? LastUpdatedIssueTime();
        }

        private class InMemoryRepository : IRepository
        {
            readonly Collection<CachedIssue> _issues = new Collection<CachedIssue>();

            public Collection<CachedIssue> GetIssues()
            {
                return _issues;
            }

            public void AddOrReplaceCachedIssue(CachedIssue flatIssue)
            {
                var cachedIssue = _issues.FirstOrDefault(x => x.Key == flatIssue.Key);
                if (cachedIssue != null)
                {
                    _issues.Remove(cachedIssue);
                }

                _issues.Insert(Math.Max(0, _issues.Count - 2), flatIssue); // inserting things out-of-order, to simulate sql's behavior of not keeping order
            }

            public DateTime? LastUpdatedIssueTime()
            {
                return _issues.Select(x => x.Updated).Max();
            }
        }

        private readonly IJiraClient _client;
        private readonly IRepository _repository;

        private DateTime? _startUpdateDate;
        public JiraLocalCache(IJiraClient client)
        {
            _client = client;
            _repository = new InMemoryRepository();
        }

        public Collection<CachedIssue> Issues
        {
            get
            {
                return _repository.GetIssues();
            }
        }

        public void SetStartDate(DateTime startDateTime)
        {
            _startUpdateDate = startDateTime;
        }

        public async Task Update()
        {
            if (!_startUpdateDate.HasValue)
            {
                throw new InvalidOperationException("Must set StartDate before first call to update.");
            }

            string projectName = "AC"; // TODO : Parametrize project

            DateTime lastUpdateDate = GetLastUpdateDateTime();

            int itemPaging = 0;
            while (true)
            {
                const int QueryLimit = 50;

                IJiraIssue[] updatedIssues = await _client.GetIssues(projectName, lastUpdateDate, QueryLimit, itemPaging);

                foreach (var issue in updatedIssues)
                {
                    CachedIssue flatIssue = await _client.RetrieveDetails(issue);

                    _repository.AddOrReplaceCachedIssue(flatIssue);
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
        private DateTime GetLastUpdateDateTime()
        {
            DateTime lastUpdateDate = _startUpdateDate.Value;

            var lastIssueUpdate = _repository.LastUpdatedIssueTime();

            if (lastIssueUpdate.HasValue)
            {
                lastUpdateDate = lastIssueUpdate.Value;
            }

            return lastUpdateDate;
        }
    }
}
