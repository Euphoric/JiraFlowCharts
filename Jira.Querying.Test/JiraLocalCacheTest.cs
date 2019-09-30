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

        /// <summary>
        /// Will fail a query if issue with given key were to be retrieved. NULL for no-op.
        /// </summary>
        public string FailIfIssueWereToBeRetrieved { get; set; }

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
        public async Task<IJiraIssue[]> GetIssues(string project, DateTime lastUpdated, int count, int skipCount)
        {
            Assert.InRange(count, 0, 50); // Must query between 0 and 50 items
            FakeJiraIssue[] returnedJiraIssues = Issues
                .Where(x => WithoutSeconds(x.Updated) >= WithoutSeconds(lastUpdated))
                .OrderBy(x => x.Updated)
                .Skip(skipCount)
                .Take(count)
                .ToArray();

            if (FailIfIssueWereToBeRetrieved != null)
            {
                var isReturningIssue = returnedJiraIssues.Any(x => x.Key == FailIfIssueWereToBeRetrieved);
                Assert.False(isReturningIssue, $"Should not have returned issue with key : {FailIfIssueWereToBeRetrieved}");
            }

            return returnedJiraIssues;
        }

        private static DateTime? WithoutSeconds(DateTime? dateTime)
        {
            if (!dateTime.HasValue)
                return null;
            var d = dateTime.Value;
            return new DateTime(d.Year, d.Month, d.Day, d.Hour, d.Minute, 0);
        }

        public async Task<CachedIssue> RetrieveDetails(IJiraIssue issue)
        {
            var fake = (FakeJiraIssue)issue;
            return new CachedIssue()
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
        private readonly FakeJiraClient _client;
        private readonly JiraLocalCache cache;
        public JiraLocalCacheTest()
        {
            _client = new FakeJiraClient();
            cache = new JiraLocalCache(_client);
        }

        [Fact]
        public async Task Update_without_start_date_is_error()
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => cache.Update());
            Assert.Equal("Must set StartDate before first call to update.", ex.Message);
        }

        [Fact]
        public async Task Updates_no_issues()
        {
            cache.SetStartDate(new DateTime(2018, 1, 1));
            await cache.Update();

            Assert.Empty(cache.Issues);
        }

        [Fact]
        public async Task Retrieves_single_issue()
        {
            _client.UpdateIssue("KEY-1");

            cache.SetStartDate(new DateTime(2018, 1, 1));
            await cache.Update();

            var cachedKeys = cache.Issues.Select(x => x.Key).ToArray();

            Assert.Equal(new[] { "KEY-1" }, cachedKeys);
        }

        [Fact]
        public async Task Retrieves_multiple_issues()
        {
            _client.UpdateIssue("KEY-1");
            _client.UpdateIssue("KEY-2");
            _client.UpdateIssue("KEY-3");

            cache.SetStartDate(new DateTime(2018, 1, 1));
            await cache.Update();

            var cachedKeys = cache.Issues.Select(x => x.Key).ToArray();

            Assert.NotStrictEqual(new[] { "KEY-1", "KEY-2", "KEY-3" }, cachedKeys);
        }

        [Fact]
        public async Task Retrieves_issues_update_field()
        {
            _client.UpdateIssue("KEY-1");
            _client.UpdateIssue("KEY-2");
            _client.UpdateIssue("KEY-3");

            cache.SetStartDate(new DateTime(2018, 1, 1));
            await cache.Update();

            var cachedKeys = cache.Issues.Select(x => x.Key).ToArray();

            var issuesByKey = cache.Issues.ToDictionary(x => x.Key, x => x.Updated.Value);

            Assert.Equal(new DateTime(2019, 1, 1), issuesByKey["KEY-1"]);
            Assert.Equal(new DateTime(2019, 1, 2), issuesByKey["KEY-2"]);
            Assert.Equal(new DateTime(2019, 1, 3), issuesByKey["KEY-3"]);
        }

        [Fact]
        public async Task Doesnt_retrieve_issue_with_older_update()
        {
            _client.UpdateIssue("KEY-1");

            cache.SetStartDate(new DateTime(2019, 1, 2));
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
            for (int i = 0; i < issueCount; i++)
            {
                _client.UpdateIssue("KEY-" + i);
            }

            cache.SetStartDate(new DateTime(2018, 1, 1));
            await cache.Update();

            var cachedKeys = cache.Issues.Select(x => x.Key).ToArray();

            Assert.NotStrictEqual(Enumerable.Range(0, issueCount).Select(i => "KEY-" + i), cachedKeys);
        }

        [Theory]
        [InlineData(199)]
        [InlineData(200)]
        [InlineData(201)]
        public async Task Retrieves_more_issues_than_is_limit_of_client_without_duplication_when_per_second_timings(int issueCount)
        {
            for (int i = 0; i < issueCount; i++)
            {
                _client.UpdateIssue("KEY-" + i, TimeSpan.FromSeconds(5));
            }

            cache.SetStartDate(new DateTime(2018, 1, 1));
            await cache.Update();

            var cachedKeys = cache.Issues.Select(x => x.Key).ToArray();

            Assert.NotStrictEqual(Enumerable.Range(0, issueCount).Select(i => "KEY-" + i), cachedKeys);
        }

        [Theory]
        [InlineData(10)]
        [InlineData(149)]
        [InlineData(199)]
        [InlineData(200)]
        [InlineData(201)]
        public async Task Retrieves_many_tasks_that_are_within_same_minute(int issueCount)
        {
            for (int i = 0; i < issueCount; i++)
            {
                _client.UpdateIssue("KEY-" + i, TimeSpan.FromSeconds(0.5));
            }

            cache.SetStartDate(new DateTime(2018, 1, 1));
            await cache.Update();

            var cachedKeys = cache.Issues.Select(x => x.Key).ToArray();

            Assert.NotStrictEqual(Enumerable.Range(0, issueCount).Select(i => "KEY-" + i), cachedKeys);
        }

        [Fact]
        public async Task Updates_issue_in_cache_when_it_was_updated_in_client1()
        {
            cache.SetStartDate(new DateTime(2018, 1, 1));

            _client.UpdateIssue("KEY-1");
            
            await cache.Update();

            _client.UpdateIssue("KEY-1");

            await cache.Update();

            var cachedIssue = Assert.Single(cache.Issues);

            Assert.Equal("KEY-1", cachedIssue.Key);
            Assert.Equal(new DateTime(2019, 1, 1), cachedIssue.Created);
            Assert.Equal(new DateTime(2019, 1, 2), cachedIssue.Updated);
        }

        [Fact]
        public async Task When_updating_doesnt_retrive_items_not_updated_since_last_update()
        {
            cache.SetStartDate(new DateTime(2018, 1, 1));

            _client.UpdateIssue("KEY-1");
            _client.UpdateIssue("KEY-2");

            await cache.Update();

            _client.UpdateIssue("KEY-2");

            _client.FailIfIssueWereToBeRetrieved = "KEY-1";

            await cache.Update();
        }

        [Fact]
        public async Task When_updating_doesnt_retrive_items_not_updated_since_last_update2()
        {
            cache.SetStartDate(new DateTime(2018, 1, 1));

            _client.UpdateIssue("KEY-1");
            _client.UpdateIssue("KEY-2");
            
            await cache.Update();

            _client.UpdateIssue("KEY-1");

            await cache.Update();

            _client.UpdateIssue("KEY-1");

            _client.FailIfIssueWereToBeRetrieved = "KEY-2";

            await cache.Update();
        }
    }
}
