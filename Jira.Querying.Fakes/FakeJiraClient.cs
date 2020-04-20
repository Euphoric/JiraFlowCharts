using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Jira.Querying
{
    public class FakeJiraClient : IJiraClient
    {
        private class FakeJiraIssue : IJiraIssue
        {
            public FakeJiraIssue(string key)
            {
                Key = key;
                StatusChanges = new List<string>();
            }

            public string Project => Key.Split('-')[0];
            public string Key { get; private set; }
            public DateTime? Created { get; set; }
            public DateTime? Updated { get; set; }
            public string Status { get; set; }
            public List<string> StatusChanges { get; }
        }

        readonly List<FakeJiraIssue> _issues = new List<FakeJiraIssue>();
        private DateTime _currentDateTime;

        /// <summary>
        /// Will fail a query if issue with given key were to be retrieved. NULL for no-op.
        /// </summary>
        public string FailIfIssueWereToBeRetrieved { get; set; }

        public string ExpectedProjectKey { get; set; }

        public FakeJiraClient()
            :this(new DateTime(2019, 1, 1))
        {

        }

        private FakeJiraClient(DateTime dateTime)
        {
            _currentDateTime = dateTime;
        }

        /// <summary>
        /// Get issues that emulates how JIRA REST API works. With all it's quirks and limitations.
        /// </summary>
        public Task<IJiraIssue[]> GetIssues(string project, DateTime lastUpdated, int count, int skipCount)
        {
            if (0 <= count && count < 50)
            {
                throw new ArgumentException("Count must be between 0 and 50", nameof(count));
            }

            if (ExpectedProjectKey != null)
            {
                if (ExpectedProjectKey != project)
                {
                    throw new InvalidOperationException($"{nameof(project)} parameter must be same as {nameof(ExpectedProjectKey)}");
                }
            }

            FakeJiraIssue[] returnedJiraIssues = _issues
                .Where(x=>x.Project == project)
                .Where(x => WithoutSeconds(x.Updated) >= WithoutSeconds(lastUpdated))
                .OrderBy(x => x.Updated)
                .Skip(skipCount)
                .Take(count)
                .ToArray();

            if (FailIfIssueWereToBeRetrieved != null)
            {
                var isReturningIssue = returnedJiraIssues.Any(x => x.Key == FailIfIssueWereToBeRetrieved);
                if (isReturningIssue)
                {
                    throw new Exception($"Should not have returned issue with key : {FailIfIssueWereToBeRetrieved}");
                }
            }

            return Task.FromResult<IJiraIssue[]>(returnedJiraIssues);
        }

        private static DateTime? WithoutSeconds(DateTime? dateTime)
        {
            if (!dateTime.HasValue)
                return null;
            var d = dateTime.Value;
            return new DateTime(d.Year, d.Month, d.Day, d.Hour, d.Minute, 0);
        }

        public Task<CachedIssue> RetrieveDetails(IJiraIssue issue)
        {
            var fake = (FakeJiraIssue)issue;
            var changes = fake.StatusChanges.Select(x=>new CachedIssueStatusChange(new DateTime(), x)).ToArray();
            CachedIssue cachedIssue = new CachedIssue()
            {
                Project = fake.Project,
                Key = fake.Key,
                Created = fake.Created,
                Updated = fake.Updated,
                Status = fake.Status,
                StatusChanges = new Collection<CachedIssueStatusChange>(changes)
            };

            return Task.FromResult(cachedIssue);
        }

        public void UpdateIssue(string key, TimeSpan? step = null, string status = null)
        {
            var existingIssue = _issues.FirstOrDefault(x => x.Key == key);
            if (existingIssue == null)
            {
                var fakeJiraIssue = new FakeJiraIssue(key) { Created = _currentDateTime, Updated = _currentDateTime, Status = status };
                fakeJiraIssue.StatusChanges.Add(status);
                _issues.Add(fakeJiraIssue);
            }
            else
            {
                existingIssue.Updated = _currentDateTime;
                existingIssue.StatusChanges.Add(status);
                existingIssue.Status = status;
            }
            _currentDateTime = _currentDateTime.Add(step ?? TimeSpan.FromDays(1));
        }
    }
}
