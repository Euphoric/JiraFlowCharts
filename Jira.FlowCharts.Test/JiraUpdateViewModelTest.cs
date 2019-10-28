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

namespace Jira.FlowCharts
{
    public class JiraUpdateViewModelTest : IAsyncLifetime
    {
        private class TestView : IJiraUpdateView
        {
            public JiraLoginParameters LoginParameters { get; set; }

            public JiraLoginParameters GetLoginParameters()
            {
                if (LoginParameters == null)
                    throw new Exception("Login parameters weren't set in test.");

                return LoginParameters;
            }
        }

        private class TestJiraClient : IJiraClient
        {
            public Task<IJiraIssue[]> GetIssues(string project, DateTime lastUpdated, int count, int skipCount)
            {
                return Task.FromResult(new IJiraIssue[0]);
            }

            public Task<CachedIssue> RetrieveDetails(IJiraIssue issue)
            {
                throw new NotImplementedException();
            }
        }

        JiraUpdateViewModel _vm;
        TestView _view;

        public JiraUpdateViewModelTest()
        {
            var repository = JiraLocalCache.CreateMemoryRepository();
            var tasksSource = new TasksSource(() => repository, _ => new TestJiraClient());
            _vm = new JiraUpdateViewModel(tasksSource);

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
        public async Task Update()
        {
            _view.LoginParameters = new JiraLoginParameters("http://url", "usrName", new System.Security.SecureString());

            await _vm.UpdateCommand.Execute().ToTask();
            Assert.Null(_vm.UpdateError);
        }
    }
}
