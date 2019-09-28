using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Jira.Querying
{
    public class FakeJiraIssue : IJiraIssue
    {
        public FakeJiraIssue(string key)
        {
            Key = key;
        }

        public DateTime? Created { get; set; }
        public DateTime? Updated { get; set; }
        public string Key { get; set; }
    }

    public class FakeJiraClient : IJiraClient
    {
        List<FakeJiraIssue> Issues = new List<FakeJiraIssue>();
        private DateTime _currentDateTime;

        public FakeJiraClient()
            :this(new DateTime(2019, 1, 1))
        {

        }

        public FakeJiraClient(DateTime dateTime)
        {
            _currentDateTime = dateTime;
        }

        /// <summary>
        /// Get issues that emulates how JIRA REST API works. With all it's quirks and limitations.
        /// </summary>
        public async Task<IJiraIssue[]> GetIssues(string project, DateTime lastUpdated, int count, int skipCount)
        {
            Assert.InRange(count, 0, 50); // Must query between 0 and 50 items
            return 
                Issues
                .Where(x => WithoutSeconds(x.Updated) >= WithoutSeconds(lastUpdated))
                .OrderBy(x=>x.Updated)
                .Skip(skipCount)
                .Take(count)
                .ToArray();
        }

        private static DateTime? WithoutSeconds(DateTime? dateTime)
        {
            if (!dateTime.HasValue)
                return null;
            var d = dateTime.Value;
            return new DateTime(d.Year, d.Month, d.Day, d.Hour, d.Minute, 0);
        }

        public async Task<FlatIssue> RetrieveDetails(IJiraIssue issue)
        {
            var fake = (FakeJiraIssue)issue;
            return new FlatIssue()
            {
                Key = fake.Key,
                Created = fake.Created,
                Updated = fake.Updated
            };
        }

        internal void UpdateIssue(string key, TimeSpan? step = null)
        {
            var existingIssue = Issues.FirstOrDefault(x => x.Key == key);
            if (existingIssue == null)
            {
                Issues.Add(new FakeJiraIssue(key) { Created = _currentDateTime, Updated = _currentDateTime });
            }
            else
            {
                existingIssue.Updated = _currentDateTime;
            }
            _currentDateTime = _currentDateTime.Add(step ?? TimeSpan.FromDays(1));
        }
    }

    
    public class JiraLocalCacheTest
    {

        [Fact]
        public async Task Updates_no_issues()
        {
            var client = new FakeJiraClient();
            JiraLocalCache cache = new JiraLocalCache(client, new DateTime(2018, 1, 1));
            await cache.Update();

            Assert.Empty(cache.Issues);
        }

        [Fact]
        public async Task Retrieves_single_issue()
        {
            var client = new FakeJiraClient();
            client.UpdateIssue("KEY-1");

            JiraLocalCache cache = new JiraLocalCache(client, new DateTime(2018, 1, 1));
            await cache.Update();

            var cachedKeys = cache.Issues.Select(x => x.Key).ToArray();

            Assert.Equal(new[] { "KEY-1" }, cachedKeys);
        }

        [Fact]
        public async Task Retrieves_multiple_issues()
        {
            var client = new FakeJiraClient();
            client.UpdateIssue("KEY-1");
            client.UpdateIssue("KEY-2");
            client.UpdateIssue("KEY-3");

            JiraLocalCache cache = new JiraLocalCache(client, new DateTime(2018, 1, 1));
            await cache.Update();

            var cachedKeys = cache.Issues.Select(x => x.Key).ToArray();

            Assert.Equal(new[] { "KEY-1", "KEY-2", "KEY-3" }, cachedKeys);
        }

        [Fact]
        public async Task Retrieves_issues_update_field()
        {
            var client = new FakeJiraClient(new DateTime(2019, 4, 1));
            client.UpdateIssue("KEY-1");
            client.UpdateIssue("KEY-2");
            client.UpdateIssue("KEY-3");

            JiraLocalCache cache = new JiraLocalCache(client, new DateTime(2018, 1, 1));
            await cache.Update();

            var cachedKeys = cache.Issues.Select(x => x.Key).ToArray();

            var issuesByKey = cache.Issues.ToDictionary(x => x.Key, x => x.Updated.Value);

            Assert.Equal(new DateTime(2019, 4, 1), issuesByKey["KEY-1"]);
            Assert.Equal(new DateTime(2019, 4, 2), issuesByKey["KEY-2"]);
            Assert.Equal(new DateTime(2019, 4, 3), issuesByKey["KEY-3"]);
        }

        [Fact]
        public async Task Doesnt_retrieve_issue_with_older_update()
        {
            var client = new FakeJiraClient(new DateTime(2019, 4, 1));
            client.UpdateIssue("KEY-1");

            JiraLocalCache cache = new JiraLocalCache(client, new DateTime(2019, 5, 1));
            await cache.Update();

            var cachedKeys = cache.Issues.Select(x => x.Key).ToArray();

            Assert.Empty(cache.Issues);
        }

        [Theory]
        [InlineData(199)]
        [InlineData(200)]
        [InlineData(201)]
        public async Task Retrieves_more_issues_than_is_limit_of_client(int issueCount)
        {
            var client = new FakeJiraClient();
            for (int i = 0; i < issueCount; i++)
            {
                client.UpdateIssue("KEY-" + i);
            }

            JiraLocalCache cache = new JiraLocalCache(client, new DateTime(2018, 1, 1));
            await cache.Update();

            var cachedKeys = cache.Issues.Select(x => x.Key).ToArray();

            Assert.Equal(Enumerable.Range(0, issueCount).Select(i => "KEY-" + i), cachedKeys);
        }

        [Theory]
        [InlineData(199)]
        [InlineData(200)]
        [InlineData(201)]
        public async Task Retrieves_more_issues_than_is_limit_of_client_without_duplication_when_per_second_timings(int issueCount)
        {
            var client = new FakeJiraClient();
            for (int i = 0; i < issueCount; i++)
            {
                client.UpdateIssue("KEY-" + i, TimeSpan.FromSeconds(5));
            }

            JiraLocalCache cache = new JiraLocalCache(client, new DateTime(2018, 1, 1));
            await cache.Update();

            var cachedKeys = cache.Issues.Select(x => x.Key).ToArray();

            Assert.Equal(Enumerable.Range(0, issueCount).Select(i => "KEY-" + i), cachedKeys);
        }

        [Theory]
        [InlineData(10)]
        [InlineData(149)]
        [InlineData(199)]
        [InlineData(200)]
        [InlineData(201)]
        public async Task Retrieves_many_tasks_that_are_within_same_minute(int issueCount)
        {
            var client = new FakeJiraClient();
            for (int i = 0; i < issueCount; i++)
            {
                client.UpdateIssue("KEY-" + i, TimeSpan.FromSeconds(0.5));
            }

            JiraLocalCache cache = new JiraLocalCache(client, new DateTime(2018, 1, 1));
            await cache.Update();

            var cachedKeys = cache.Issues.Select(x => x.Key).ToArray();

            Assert.Equal(Enumerable.Range(0, issueCount).Select(i => "KEY-" + i), cachedKeys);
        }

        [Fact]
        public async Task Updates_issue_in_cache_when_it_was_updated_in_client1()
        {
            var client = new FakeJiraClient(new DateTime(2019, 1, 1));
            JiraLocalCache cache = new JiraLocalCache(client, new DateTime(2018, 1, 1));

            client.UpdateIssue("KEY-1");
            
            await cache.Update();

            client.UpdateIssue("KEY-1");

            await cache.Update();

            var cachedIssue = Assert.Single(cache.Issues);

            Assert.Equal("KEY-1", cachedIssue.Key);
            Assert.Equal(new DateTime(2019, 1, 1), cachedIssue.Created);
            Assert.Equal(new DateTime(2019, 1, 2), cachedIssue.Updated);
        }
    }
}
