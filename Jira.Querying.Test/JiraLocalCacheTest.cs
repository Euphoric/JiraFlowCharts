using AutoFixture;
using AutoFixture.Xunit2;
using Jira.Querying.Sqlite;
using KellermanSoftware.CompareNetObjects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public string Key { get; private set; }
        public DateTime? Created { get; set; }
        public DateTime? Updated { get; set; }
    }

    public class FakeJiraClient : IJiraClient
    {
        readonly List<FakeJiraIssue> Issues = new List<FakeJiraIssue>();
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
        public Task<IJiraIssue[]> GetIssues(string project, DateTime lastUpdated, int count, int skipCount)
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
            CachedIssue cachedIssue = new CachedIssue()
            {
                Key = fake.Key,
                Created = fake.Created,
                Updated = fake.Updated,
                StatusChanges = new Collection<CachedIssueStatusChange>()
            };

            return Task.FromResult(cachedIssue);
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
    
    public class JiraLocalCacheTestSqlite : JiraLocalCacheTest
    {
        public JiraLocalCacheTestSqlite()
            :base(new SqliteJiraLocalCacheRepository())
        {
        }
    }

    public class JiraLocalCacheTest : IDisposable
    {
        private readonly FakeJiraClient _client;

        protected JiraLocalCache.IRepository Repository { get; }
        protected JiraLocalCache Cache { get; }

        public JiraLocalCacheTest()
            :this(JiraLocalCache.CreateMemoryRepository())
        {
        }

        public void Dispose()
        {
            Cache.Dispose();
        }

        protected JiraLocalCacheTest(JiraLocalCache.IRepository repository)
        {
            _client = new FakeJiraClient();
            Repository = repository;
            Cache = new JiraLocalCache(repository);
        }

        private Task CacheUpdate(DateTime? startUpdateDate = null)
        {
            return Cache.Update(_client, startUpdateDate ?? new DateTime(2018, 1, 1));
        }

        [Fact]
        public async Task Update_without_initializing_is_error()
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(()=>CacheUpdate());
            Assert.Equal("Must call Initialize before updating.", ex.Message);
        }

        [Fact]
        public void Repository_is_disposed_after_cache_is_disposed_when_not_initialized()
        {
            Assert.False(Repository.IsDisposed);
            Cache.Dispose();
            Assert.True(Repository.IsDisposed);
        }

        [Fact]
        public async Task Repository_is_disposed_after_cache_is_disposed_when_initialized()
        {
            await Cache.Initialize();

            Assert.False(Repository.IsDisposed);
            Cache.Dispose();
            Assert.True(Repository.IsDisposed);
        }

        [Fact]
        public async Task Update_parameter_check()
        {
            await Cache.Initialize();
            await Assert.ThrowsAsync<ArgumentNullException>(()=> Cache.Update(null, new DateTime(2018, 1, 1)));
        }

        [Fact]
        public async Task Updates_no_issues()
        {
            await Cache.Initialize();
            await CacheUpdate();

            Assert.Empty(await Cache.GetIssues());
        }

        [Fact]
        public async Task Retrieves_single_issue()
        {
            _client.UpdateIssue("KEY-1");

            await Cache.Initialize();
            await CacheUpdate();

            var cachedKeys = (await Cache.GetIssues()).Select(x => x.Key).ToArray();

            Assert.Equal(new[] { "KEY-1" }, cachedKeys);
        }

        [Fact]
        public async Task Retrieves_multiple_issues()
        {
            _client.UpdateIssue("KEY-1");
            _client.UpdateIssue("KEY-2");
            _client.UpdateIssue("KEY-3");

            await Cache.Initialize();
            await CacheUpdate();

            var cachedKeys = (await Cache.GetIssues()).Select(x => x.Key).ToArray();

            Assert.NotStrictEqual(new[] { "KEY-1", "KEY-2", "KEY-3" }, cachedKeys);
        }

        [Fact]
        public async Task Retrieves_issues_update_field()
        {
            _client.UpdateIssue("KEY-1");
            _client.UpdateIssue("KEY-2");
            _client.UpdateIssue("KEY-3");

            await Cache.Initialize();
            await CacheUpdate();

            var cachedKeys = (await Cache.GetIssues()).Select(x => x.Key).ToArray();

            var issuesByKey = (await Cache.GetIssues()).ToDictionary(x => x.Key, x => x.Updated.Value);

            Assert.Equal(new DateTime(2019, 1, 1), issuesByKey["KEY-1"]);
            Assert.Equal(new DateTime(2019, 1, 2), issuesByKey["KEY-2"]);
            Assert.Equal(new DateTime(2019, 1, 3), issuesByKey["KEY-3"]);
        }

        [Fact]
        public async Task Doesnt_retrieve_issue_with_older_update()
        {
            _client.UpdateIssue("KEY-1");

            await Cache.Initialize();
            await CacheUpdate(new DateTime(2019, 1, 2));

            var cachedKeys = (await Cache.GetIssues()).Select(x => x.Key).ToArray();

            Assert.Empty(await Cache.GetIssues());
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

            await Cache.Initialize();
            await CacheUpdate();

            var cachedKeys = (await Cache.GetIssues()).Select(x => x.Key).ToArray();

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

            await Cache.Initialize();
            await CacheUpdate();

            var cachedKeys = (await Cache.GetIssues()).Select(x => x.Key).ToArray();

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

            await Cache.Initialize();
            await CacheUpdate();

            var cachedKeys = (await Cache.GetIssues()).Select(x => x.Key).ToArray();

            Assert.NotStrictEqual(Enumerable.Range(0, issueCount).Select(i => "KEY-" + i), cachedKeys);
        }

        [Fact]
        public async Task Updates_issue_in_cache_when_it_was_updated_in_client1()
        {
            await Cache.Initialize();

            _client.UpdateIssue("KEY-1");
            
            await CacheUpdate();

            _client.UpdateIssue("KEY-1");

            await CacheUpdate();

            var cachedIssue = Assert.Single(await Cache.GetIssues());

            Assert.Equal("KEY-1", cachedIssue.Key);
            Assert.Equal(new DateTime(2019, 1, 1), cachedIssue.Created);
            Assert.Equal(new DateTime(2019, 1, 2), cachedIssue.Updated);
        }

        [Fact]
        public async Task When_updating_doesnt_retrive_items_not_updated_since_last_update()
        {
            await Cache.Initialize();

            _client.UpdateIssue("KEY-1");
            _client.UpdateIssue("KEY-2");

            await CacheUpdate();

            _client.UpdateIssue("KEY-2");

            _client.FailIfIssueWereToBeRetrieved = "KEY-1";

            await CacheUpdate();
        }

        [Fact]
        public async Task When_updating_doesnt_retrive_items_not_updated_since_last_update2()
        {
            await Cache.Initialize();

            _client.UpdateIssue("KEY-1");
            _client.UpdateIssue("KEY-2");
            
            await CacheUpdate();

            _client.UpdateIssue("KEY-1");

            await CacheUpdate();

            _client.UpdateIssue("KEY-1");

            _client.FailIfIssueWereToBeRetrieved = "KEY-2";

            await CacheUpdate();
        }

        [Theory, AutoData]
        public async Task Repository_saves_whole_issue(CachedIssue issue)
        {
            await Repository.Initialize();

            await Repository.AddOrReplaceCachedIssue(issue);

            var retrievedIssue = (await Repository.GetIssues()).SingleOrDefault();

            issue.ShouldCompare(retrievedIssue);
        }

        [Fact]
        public async Task Repository_saves_many_issue()
        {
            Fixture fixture = new Fixture();
            var issues = fixture.CreateMany<CachedIssue>(10);

            await Repository.Initialize();
            foreach (var issue in issues.OrderBy(x => x.Key))
            {
                await Repository.AddOrReplaceCachedIssue(issue);
            }

            var retrievedIssues = await Repository.GetIssues();

            issues.OrderBy(x=>x.Key).ToArray().ShouldCompare(retrievedIssues.OrderBy(x => x.Key).ToArray());
        }

        [Fact]
        public async Task Repository_replaces_issue()
        {
            Fixture fixture = new Fixture();
            var issue1 = fixture.Create<CachedIssue>();
            issue1.Key = "KEY-1";
            var issue2 = fixture.Create<CachedIssue>();
            issue2.Key = "KEY-1";

            await Repository.Initialize();

            await Repository.AddOrReplaceCachedIssue(issue1);
            await Repository.AddOrReplaceCachedIssue(issue2);

            var retrievedIssue = (await Repository.GetIssues()).SingleOrDefault();

            issue2.ShouldCompare(retrievedIssue);
        }
    }
}
