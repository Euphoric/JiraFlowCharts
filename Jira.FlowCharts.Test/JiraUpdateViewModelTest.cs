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

        private class TestJiraCacheAdapter : ITasksSourceJiraCacheAdapter
        {
            public JiraLoginParameters ExpectedLoginParameters { get; internal set; }

            public Task<List<CachedIssue>> GetIssues()
            {
                return Task.FromResult(new List<CachedIssue>());
            }

            public Task UpdateIssues(JiraLoginParameters jiraLoginParameters)
            {
                if (ExpectedLoginParameters != null)
                {
                    Assert.Equal(ExpectedLoginParameters, jiraLoginParameters);
                }

                return Task.CompletedTask;
            }
        }

        JiraUpdateViewModel _vm;
        TestView _view;
        TestJiraCacheAdapter _jiraCacheAdapter;

        public JiraUpdateViewModelTest()
        {
            _jiraCacheAdapter = new TestJiraCacheAdapter();

            var tasksSource = new TasksSource(_jiraCacheAdapter);
            _vm = new JiraUpdateViewModel(tasksSource);

            _view = new TestView();
            (_vm as IViewAware).AttachView(_view);

            _view.LoginParameters = new JiraLoginParameters("http://url", "usrName", new System.Security.SecureString());
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
            _jiraCacheAdapter.ExpectedLoginParameters = _view.LoginParameters;

            await _vm.UpdateCommand.Execute().ToTask();
            Assert.Null(_vm.UpdateError);
        }
    }
}
