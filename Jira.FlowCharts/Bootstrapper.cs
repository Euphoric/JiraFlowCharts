using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using Caliburn.Micro;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Formatting.Compact;

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

            sc.AddLogging(cfg => 
                cfg
                    .SetMinimumLevel(LogLevel.Trace)
                    .AddSerilog(CreateSerilogLogger(), true));

            _container = sc.BuildServiceProvider();
        }

        private static Logger CreateSerilogLogger()
        {
#if DEBUG
            var logsPath = "Logs";
#else
            var logsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "JiraFlowMetrics", "Logs");
#endif

            return new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.File(new CompactJsonFormatter(), Path.Combine(logsPath, "log.json"), rollingInterval: RollingInterval.Day)
                .MinimumLevel.Verbose()
                .CreateLogger();
        }

        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            var logger = _container.GetRequiredService<ILoggerFactory>().CreateLogger<Bootstrapper>();
            logger.LogInformation("Startup");

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