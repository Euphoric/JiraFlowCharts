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
        private class Repository
        {
            public Collection<CachedIssue> Issues { get; private set; } = new Collection<CachedIssue>();

            public void AddOrReplaceCachedIssue(CachedIssue flatIssue)
            {
                var cachedIssue = Issues.FirstOrDefault(x => x.Key == flatIssue.Key);
                if (cachedIssue != null)
                {
                    Issues.Remove(cachedIssue);
                }

                Issues.Add(flatIssue);
            }
        }

        private readonly IJiraClient _client;
        private readonly DateTime _startUpdateDate;
        private readonly Repository _repository;

        public JiraLocalCache(IJiraClient client, DateTime startUpdateDate)
        {
            _client = client;
            _startUpdateDate = startUpdateDate;
            _repository = new Repository();
        }

        public Collection<CachedIssue> Issues
        {
            get
            {
                return _repository.Issues;
            }
        }

        public async Task Update()
        {
            string projectName = "AC"; // TODO : Parametrize project

            int itemPaging = 0;
            while (true)
            {
                const int QueryLimit = 50;

                IJiraIssue[] updatedIssues = await _client.GetIssues(projectName, _startUpdateDate, QueryLimit, itemPaging);

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
