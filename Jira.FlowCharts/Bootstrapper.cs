using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Threading;
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
        private ILogger<Bootstrapper> _logger;

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

            _logger = _container.GetRequiredService<ILoggerFactory>().CreateLogger<Bootstrapper>();
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

        protected override async void OnStartup(object sender, StartupEventArgs e)
        {
            _logger.LogInformation("Startup");

            try
            {
                await DisplayRootViewFor<MainViewModel>();
            }
            catch (Exception exception)
            {
                _logger.LogCritical(exception, "Error during startup. Exiting.");
                Application.Shutdown(1);
            }
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

        protected override void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            _logger.LogError(e.Exception, "Unhandled exception.");

            base.OnUnhandledException(sender, e);
        }
    }
}