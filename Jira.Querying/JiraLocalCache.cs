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

                _issues.Add(flatIssue);
            }
        }

        private readonly IJiraClient _client;
        private readonly IRepository _repository;

        private DateTime? _startUpdateDate;
        public JiraLocalCache(IJiraClient client, DateTime? startUpdateDate = null)
        {
            _client = client;
            _startUpdateDate = startUpdateDate;
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

            int itemPaging = 0;
            while (true)
            {
                const int QueryLimit = 50;

                IJiraIssue[] updatedIssues = await _client.GetIssues(projectName, _startUpdateDate.Value, QueryLimit, itemPaging);

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
    }
}
