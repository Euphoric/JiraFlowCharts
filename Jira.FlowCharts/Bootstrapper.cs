using System;
using System.Collections.Generic;
using System.Windows;
using Caliburn.Micro;
using Microsoft.Extensions.DependencyInjection;

namespace Jira.FlowCharts
{
    public class Bootstrapper : BootstrapperBase
    {
        private ServiceProvider _container;

        public Bootstrapper()
        {
            Initialize();
        }

        protected override void Configure()
        {
            ServiceCollection sc = new ServiceCollection();

            sc.AddSingleton<IWindowManager, WindowManager>();
            sc.AddTransient<MainViewModel>();

            _container = sc.BuildServiceProvider();
        }

        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            DisplayRootViewFor<MainViewModel>();
        }

        protected override object GetInstance(Type service, string key)
        {
            return _container.GetRequiredService(service);
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            return _container.GetServices(service);
        }

        protected override void BuildUp(object instance)
        {
            throw new NotSupportedException("Service provider doesn't have a buildup functionality.");
        }
    }
}