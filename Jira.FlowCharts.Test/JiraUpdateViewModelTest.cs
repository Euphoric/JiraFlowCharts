using Caliburn.Micro;
using Jira.FlowCharts.JiraUpdate;
using Jira.Querying;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using System.Reactive.Linq;
using System.Security;
using IScreen = Caliburn.Micro.IScreen;

namespace Jira.FlowCharts
{
    public class JiraUpdateViewModelTest : IAsyncLifetime
    {
        private class TestView : IJiraUpdateView
        {
            public SecureString LoginPassword { get; set; }

            public SecureString GetLoginPassword()
            {
                return LoginPassword;
            }
        }

        private class TestCurrentTime : ICurrentTime
        {
            public DateTime UtcNow { get; set; }
        }

        JiraUpdateViewModel _vm;
        TestView _view;
        TestJiraCacheAdapter _jiraCacheAdapter;
        TestCurrentTime _currentTime;

        public JiraUpdateViewModelTest()
        {
            _jiraCacheAdapter = new TestJiraCacheAdapter();

            var tasksSource = new TasksSource(_jiraCacheAdapter, new MemoryStatesRepository(new string[0], new string[0]));
            _currentTime = new TestCurrentTime();
            _vm = new JiraUpdateViewModel(tasksSource, _currentTime);

            _view = new TestView();
            (_vm as IViewAware).AttachView(_view);
        }

        public async Task InitializeAsync()
        {
            await (_vm as IScreen).ActivateAsync();
        }

        public async Task DisposeAsync()
        {
            await (_vm as IScreen).DeactivateAsync(false);
        }

        [Fact]
        public Task Activating_updates_current_status()
        {
            Assert.Equal(0, _vm.CachedIssuesCount);
            Assert.Null(_vm.LastUpdatedIssue);

            return Task.CompletedTask;
        }

        [Fact]
        public async Task Update_sends_right_parameters_and_doesnt_error()
        {
            var jiraPassword = new SecureString();

            // user enters values
            _vm.JiraUrl = "http://url";
            _vm.JiraUsername = "usrName";
            _vm.ProjectKey = "Abcd";
            _view.LoginPassword = jiraPassword;

            // expected parameters passed into adapter
            _jiraCacheAdapter.ExpectedLoginParameters = new JiraLoginParameters("http://url", "usrName", jiraPassword);
            _jiraCacheAdapter.ExpectedProjectKey = "Abcd";

            // execute and assert
            await _vm.UpdateCommand.Execute().ToTask();
            Assert.Null(_vm.UpdateError);
        }

        [Fact]
        public async Task Update_refreshes_display()
        {
            _jiraCacheAdapter.IssuesToUpdateWith.Add(new CachedIssue() { Key = "KEY-1", Updated = new DateTime(2019, 1, 1)});
            _jiraCacheAdapter.IssuesToUpdateWith.Add(new CachedIssue() { Key = "KEY-2", Updated = new DateTime(2019, 2, 2)});

            await _vm.UpdateCommand.Execute().ToTask();
            Assert.Null(_vm.UpdateError);

            Assert.Equal(2, _vm.CachedIssuesCount);
            Assert.Equal(new DateTime(2019, 2, 2), _vm.LastUpdatedIssue);
        }

        [Fact]
        public async Task Zero_update_issues_progress()
        {
            using (var progressUpdates = _vm.RecordProperty<double>(nameof(_vm.UpdateProgress)))
            {
                await _vm.UpdateCommand.Execute().ToTask();
                Assert.Null(_vm.UpdateError);

                Assert.Equal(
                    new double[] { 0, 100 },
                    progressUpdates.ObservedItems);
            }
        }

        /// <summary>
        /// Asserts that progress update starts at 0 ends at 100 and has raising tendency for all updates.
        /// </summary>
        private static void AssertProgressUpdateIsCorrect(IEnumerable<double> observedProgressUpdates, int expectedUpdateCounts, double maxDistance)
        {
            Assert.Equal(0, observedProgressUpdates.First());
            Assert.Equal(100, observedProgressUpdates.Last());
            // first reported element should be equal to 1
            Assert.Equal(1, observedProgressUpdates.ElementAt(1));

            var itemUpdates = observedProgressUpdates.Skip(1).Take(observedProgressUpdates.Count() - 2).ToList();
            Assert.Equal(expectedUpdateCounts, itemUpdates.Count);

            double sumDist = 0;
            double prevUpdate = 0;
            foreach (var progressUpdate in itemUpdates)
            {
                Assert.InRange(progressUpdate, 0, 100);
                Assert.True(prevUpdate < progressUpdate);
                sumDist += progressUpdate - prevUpdate;
                prevUpdate = progressUpdate;
            }

            var maxDist = Distances(observedProgressUpdates).Max();
            Assert.True(maxDistance > maxDist);
        }

        private static IEnumerable<double> Distances(IEnumerable<double> observedItems)
        {
            return observedItems.Zip(observedItems.Skip(1), (a, b) => b - a).ToArray();
        }

        [Fact]
        public async Task Single_update_issues_progress()
        {
            using (var progressUpdates = _vm.RecordProperty<double>(nameof(_vm.UpdateProgress)))
            {
                _jiraCacheAdapter.IssuesToUpdateWith.Add(new CachedIssue() { Key = "KEY-1", Updated = new DateTime(2019, 1, 1) });

                await _vm.UpdateCommand.Execute().ToTask();
                Assert.Null(_vm.UpdateError);

                AssertProgressUpdateIsCorrect(progressUpdates.ObservedItems, 1, 100);
            }
        }

        [Fact]
        public async Task Multiple_update_issues_progress()
        {
            using (var progressUpdates = _vm.RecordProperty<double>(nameof(_vm.UpdateProgress)))
            {
                _jiraCacheAdapter.IssuesToUpdateWith.Add(new CachedIssue() { Key = "KEY-1", Updated = new DateTime(2019, 1, 1) });
                _jiraCacheAdapter.IssuesToUpdateWith.Add(new CachedIssue() { Key = "KEY-2", Updated = new DateTime(2019, 1, 2) });

                _currentTime.UtcNow = new DateTime(2019, 1, 3);

                await _vm.UpdateCommand.Execute().ToTask();
                Assert.Null(_vm.UpdateError);

                AssertProgressUpdateIsCorrect(progressUpdates.ObservedItems, 2, 60);
            }
        }

        [Fact]
        public async Task Many_update_issues_progress()
        {
            using (var progressUpdates = _vm.RecordProperty<double>(nameof(_vm.UpdateProgress)))
            {
                var issuesCount = 200;
                for (int i = 0; i < issuesCount; i++)
                {
                    _jiraCacheAdapter.IssuesToUpdateWith.Add(new CachedIssue()
                    {
                        Key = "KEY-" + i,
                        Updated = new DateTime(2019, 1, 1).AddMinutes(i)
                    });
                }

                _currentTime.UtcNow = new DateTime(2019, 1, 1).AddMinutes(issuesCount + 1);

                await _vm.UpdateCommand.Execute().ToTask();
                Assert.Null(_vm.UpdateError);

                AssertProgressUpdateIsCorrect(progressUpdates.ObservedItems, issuesCount, 1.5);
            }
        }

        [Fact]
        public async Task Updating_multiple_times_restarts_progress()
        {
            _jiraCacheAdapter.IssuesToUpdateWith.Add(new CachedIssue() { Key = "KEY-1", Updated = new DateTime(2019, 1, 1) });

            await _vm.UpdateCommand.Execute().ToTask();
            Assert.Null(_vm.UpdateError);

            using (var progressUpdates = _vm.RecordProperty<double>(nameof(_vm.UpdateProgress)))
            {
                _jiraCacheAdapter.IssuesToUpdateWith.Add(new CachedIssue() { Key = "KEY-1", Updated = new DateTime(2019, 10, 2) });

                _currentTime.UtcNow = new DateTime(2019, 10, 3);

                await _vm.UpdateCommand.Execute().ToTask();
                Assert.Null(_vm.UpdateError);

                AssertProgressUpdateIsCorrect(progressUpdates.ObservedItems, 1, 100);
            }
        }

        [Fact]
        public async Task Reports_key_of_last_updated_issues_key_as_update_runs()
        {
            using (var progressUpdates = _vm.RecordProperty<string>(nameof(_vm.LastUpdatedKey)))
            {
                _jiraCacheAdapter.IssuesToUpdateWith.Add(new CachedIssue() { Key = "KEY-11", Updated = new DateTime(2019, 1, 1) });
                _jiraCacheAdapter.IssuesToUpdateWith.Add(new CachedIssue() { Key = "KEY-2", Updated = new DateTime(2019, 1, 2) });

                _currentTime.UtcNow = new DateTime(2019, 1, 3);

                await _vm.UpdateCommand.Execute().ToTask();
                Assert.Null(_vm.UpdateError);

                Assert.Equal(new string[] {"KEY-11", "KEY-2", "Done"}, progressUpdates.ObservedItems);
            }
        }
    }
}
