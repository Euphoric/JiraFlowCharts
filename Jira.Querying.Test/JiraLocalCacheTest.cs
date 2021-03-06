﻿using AutoFixture;
using AutoFixture.Xunit2;
using Jira.Querying.Sqlite;
using KellermanSoftware.CompareNetObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Jira.Querying
{
    public class JiraLocalCacheTestSqlite : JiraLocalCacheTest
    {
        public JiraLocalCacheTestSqlite()
            : base(new SqliteJiraLocalCacheRepository())
        {
        }
    }

    public struct ReportedIssueUpdate
    {
        public ReportedIssueUpdate(string key, DateTime updated)
        {
            Key = key;
            Updated = updated;
        }

        public string Key { get; private set; }
        public DateTime Updated { get; private set; }

        public override string ToString()
        {
            return $"{Key};{Updated}";
        }
    }

    public class MemoryCacheUpdateProgress : ICacheUpdateProgress
    {
        List<ReportedIssueUpdate> _updatedIssues = new List<ReportedIssueUpdate>();

        public void UpdatedIssue(string key, DateTime updated)
        {
            _updatedIssues.Add(new ReportedIssueUpdate(key, updated));
        }

        public void AssertReported(params ReportedIssueUpdate[] expectedReports)
        {
            Assert.Equal(expectedReports, _updatedIssues);

            _updatedIssues.Clear();
        }
    }

    public class JiraLocalCacheTest : IDisposable
    {
        private readonly FakeJiraClient _client;

        protected JiraLocalCache.IRepository Repository { get; }
        protected JiraLocalCache Cache { get; }

        private readonly MemoryCacheUpdateProgress _cacheUpdateProgress;

        public JiraLocalCacheTest()
            : this(JiraLocalCache.CreateMemoryRepository())
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
            _cacheUpdateProgress = new MemoryCacheUpdateProgress();
        }

        private Task CacheUpdate(DateTime? startUpdateDate = null, string projectKey = "KEY")
        {
            return Cache.Update(_client, startUpdateDate ?? new DateTime(2018, 1, 1), projectKey, _cacheUpdateProgress);
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
            Assert.False(Repository.IsDisposed);
            Cache.Dispose();
            Assert.True(Repository.IsDisposed);
        }

        [Fact]
        public async Task Update_parameter_check()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => Cache.Update(null, new DateTime(2018, 1, 1), "AB"));
        }

        [Fact]
        public async Task Project_key_cannot_be_empty()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => Cache.Update(_client, new DateTime(2018, 1, 1), ""));
        }

        [Fact]
        public async Task Updates_no_issues()
        {
            await CacheUpdate();

            Assert.Empty(await Cache.GetIssues());
        }

        [Fact]
        public async Task Update_passes_right_project_key()
        {
            _client.ExpectedProjectKey = "EFG";

            await CacheUpdate(projectKey: "EFG");

            Assert.Empty(await Cache.GetIssues());
        }

        [Fact]
        public async Task Retrieves_single_issue()
        {
            _client.UpdateIssue("KEY-1");

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

            await CacheUpdate();

            var cachedKeys = (await Cache.GetIssues()).Select(x => x.Key).ToArray();

            Assert.NotStrictEqual(Enumerable.Range(0, issueCount).Select(i => "KEY-" + i), cachedKeys);
        }

        [Fact]
        public async Task Updates_issue_in_cache_when_it_was_updated_in_client1()
        {
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

            issues.OrderBy(x => x.Key).ToArray().ShouldCompare(retrievedIssues.OrderBy(x => x.Key).ToArray());
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

        [Fact]
        public async Task Issues_reported_when_updated()
        {
            _client.UpdateIssue("KEY-1");
            _client.UpdateIssue("KEY-2");
            await CacheUpdate();
            _cacheUpdateProgress.AssertReported(
                new ReportedIssueUpdate("KEY-1", new DateTime(2019, 1, 1)),
                new ReportedIssueUpdate("KEY-2", new DateTime(2019, 1, 2))
                );

            _client.UpdateIssue("KEY-3");
            _client.UpdateIssue("KEY-4");
            await CacheUpdate();
            _cacheUpdateProgress.AssertReported(
                new ReportedIssueUpdate("KEY-2", new DateTime(2019, 1, 2)),
                new ReportedIssueUpdate("KEY-3", new DateTime(2019, 1, 3)),
                new ReportedIssueUpdate("KEY-4", new DateTime(2019, 1, 4))
                );

            _client.UpdateIssue("KEY-1");
            _client.UpdateIssue("KEY-4");
            await CacheUpdate();
            _cacheUpdateProgress.AssertReported(
                new ReportedIssueUpdate("KEY-1", new DateTime(2019, 1, 5)),
                new ReportedIssueUpdate("KEY-4", new DateTime(2019, 1, 6))
                );
        }

        [Fact]
        public async Task No_projects_when_empty()
        {
            var projects = await Cache.GetProjects();

            Assert.Empty(projects);
        }

        [Theory]
        [InlineData("KEY")]
        [InlineData("PROJ")]
        public async Task Returns_project_after_update(string project)
        {
            _client.UpdateIssue(project + "-1");

            await CacheUpdate(projectKey: project);

            var projects = await Cache.GetProjects();

            Assert.Equal(new[] { project }, projects.Select(x => x.Key));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(11)]
        public async Task Returns_project_statistics(int issueCount)
        {
            string projectKey = "KEY";

            for (int i = 1; i <= issueCount; i++)
            {
                _client.UpdateIssue(projectKey + "-" + i);
            }

            await CacheUpdate(projectKey: projectKey);

            var projects = await Cache.GetProjects();

            var project = Assert.Single(projects);
            Assert.NotNull(project);

            Assert.Equal(issueCount, project.IssueCount);
            Assert.Equal(new DateTime(2019, 1, 1).AddDays(issueCount - 1), project.LastUpdatedTime);
        }

        [Fact]
        public async Task Updates_only_specific_project()
        {
            _client.UpdateIssue("A-1");
            _client.UpdateIssue("B-1");
            _client.UpdateIssue("C-1");

            await CacheUpdate(projectKey: "A");

            var retrievedIssues = await Cache.GetIssues();

            var issueKeys = retrievedIssues.Select(x => x.Key).ToArray();
            Assert.Equal(new[] { "A-1" }, issueKeys);
        }

        [Fact]
        public async Task Returns_all_updated_projects()
        {
            _client.UpdateIssue("A-1");
            _client.UpdateIssue("B-1");
            _client.UpdateIssue("C-1");

            await CacheUpdate(projectKey: "A");
            await CacheUpdate(projectKey: "B");
            await CacheUpdate(projectKey: "C");

            var retrievedIssues = await Cache.GetIssues();

            var issueKeys = retrievedIssues.Select(x => x.Key).ToArray();
            Assert.Equal(new[] { "A-1", "B-1", "C-1" }, issueKeys.OrderBy(x => x));
        }

        [Fact]
        public async Task Last_update_times_are_independent_per_project()
        {
            _client.UpdateIssue("A-1");
            _client.UpdateIssue("B-1");

            await CacheUpdate(projectKey: "B");
            await CacheUpdate(projectKey: "A");

            var retrievedIssues = await Cache.GetIssues();

            var issueKeys = retrievedIssues.Select(x => x.Key).ToArray();
            Assert.Equal(new[] { "A-1", "B-1" }, issueKeys.OrderBy(x => x));
        }

        [Fact]
        public async Task Issues_are_empty_for_nonexisting_project()
        {
            var issueKeys = (await Cache.GetIssues("A")).Select(x => x.Key).ToArray();
            Assert.Empty(issueKeys);
        }

        [Fact]
        public async Task Issues_are_returned_for_project()
        {
            _client.UpdateIssue("A-1");
            await CacheUpdate(projectKey: "A");

            var issueKeys = (await Cache.GetIssues("A")).Select(x => x.Key).ToArray();
            Assert.Equal(new[] { "A-1" }, issueKeys);
        }

        [Fact]
        public async Task Issues_are_returned_only_for_specified_project()
        {
            _client.UpdateIssue("A-1");
            _client.UpdateIssue("B-1");

            await CacheUpdate(projectKey: "A");

            var issueAKeys = (await Cache.GetIssues("A")).Select(x => x.Key).ToArray();
            Assert.Equal(new[] { "A-1" }, issueAKeys);

            var issueBKeys = (await Cache.GetIssues("B")).Select(x => x.Key).ToArray();
            Assert.Empty(issueBKeys);
        }

        [Fact]
        public async Task No_status_when_no_issues()
        {
            string[] statuses = await Cache.GetStatuses();

            Assert.Empty(statuses);
        }

        [Fact]
        public async Task Returns_status_of_issue()
        {
            _client.UpdateIssue("A-1", status: "In Progress");

            await CacheUpdate(projectKey: "A");

            string[] statuses = await Cache.GetStatuses();

            Assert.Equal(new[] { "In Progress" }, statuses);
        }

        [Fact]
        public async Task Returns_multiple_statuses_of_issues()
        {
            _client.UpdateIssue("A-1", status: "In Progress");
            _client.UpdateIssue("A-2", status: "In Development");
            _client.UpdateIssue("A-3", status: "Done");

            await CacheUpdate(projectKey: "A");

            string[] statuses = await Cache.GetStatuses();

            Assert.Equal(new[] { "Done", "In Development", "In Progress" }, statuses.OrderBy(x => x));
        }

        [Fact]
        public async Task Statuses_are_distinct()
        {
            _client.UpdateIssue("A-1", status: "In Progress");
            _client.UpdateIssue("A-2", status: "In Progress");
            _client.UpdateIssue("A-3", status: "Done");
            _client.UpdateIssue("A-4", status: "Done");

            await CacheUpdate(projectKey: "A");

            string[] statuses = await Cache.GetStatuses();

            Assert.Equal(new[] { "Done", "In Progress" }, statuses.OrderBy(x => x));
        }

        [Fact]
        public async Task Returns_statuses_from_status_changes()
        {
            _client.UpdateIssue("A-1", status: "In Progress");
            _client.UpdateIssue("A-1", status: "Done");

            await CacheUpdate(projectKey: "A");

            string[] statuses = await Cache.GetStatuses();

            Assert.Equal(new[] { "Done", "In Progress" }, statuses.OrderBy(x => x));
        }

        [Fact]
        public async Task Returns_multiple_statuses_from_multiple_issues_and_multiple_changes()
        {
            _client.UpdateIssue("A-1", status: "In Progress");
            _client.UpdateIssue("A-1", status: "Done");

            _client.UpdateIssue("A-2", status: "In Dev");
            _client.UpdateIssue("A-2", status: "In QA");
            _client.UpdateIssue("A-2", status: "Ready for Done");

            await CacheUpdate(projectKey: "A");

            string[] statuses = await Cache.GetStatuses();

            Assert.Equal(new[] { "Done", "In Dev", "In Progress", "In QA", "Ready for Done" }, statuses.OrderBy(x => x));
        }

        [Fact]
        public async Task Statuses_are_distinct_across_multiple_issues_and_status_changes()
        {
            _client.UpdateIssue("A-1", status: "In Progress");
            _client.UpdateIssue("A-2", status: "In QA");
            _client.UpdateIssue("A-1", status: "Done");

            _client.UpdateIssue("A-2", status: "In Progress");
            _client.UpdateIssue("A-2", status: "In Dev");
            _client.UpdateIssue("A-2", status: "In QA");
            _client.UpdateIssue("A-2", status: "In Dev");
            _client.UpdateIssue("A-2", status: "In QA");
            _client.UpdateIssue("A-2", status: "Ready for Done");

            await CacheUpdate(projectKey: "A");

            string[] statuses = await Cache.GetStatuses();

            Assert.Equal(new[] { "Done", "In Dev", "In Progress", "In QA", "Ready for Done" }, statuses.OrderBy(x => x));
        }
    }
}
