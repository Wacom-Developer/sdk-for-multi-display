using Microsoft.Extensions.Logging;
using Serilog;
using System;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Wacom.Kiosk.IntegratorUI
{
    public class Serilogger : ILogger
    {
        private readonly Serilog.Core.Logger seriLogger = null;

        public Serilogger()
        {
            seriLogger = new LoggerConfiguration()
                                .MinimumLevel.Verbose()
                                .WriteTo.Console()
                                .WriteTo.File("IntegratorLog.txt")
                                .CreateLogger();
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    return seriLogger.IsEnabled(Serilog.Events.LogEventLevel.Verbose);
                case LogLevel.Debug:
                    return seriLogger.IsEnabled(Serilog.Events.LogEventLevel.Debug);
                case LogLevel.Information:
                    return seriLogger.IsEnabled(Serilog.Events.LogEventLevel.Information);
                case LogLevel.Warning:
                    return seriLogger.IsEnabled(Serilog.Events.LogEventLevel.Warning);
                case LogLevel.Error:
                    return seriLogger.IsEnabled(Serilog.Events.LogEventLevel.Error);
                case LogLevel.Critical:
                    return seriLogger.IsEnabled(Serilog.Events.LogEventLevel.Fatal);
                default:
                    return false;
            }
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            string logMessage = formatter.Invoke(state, exception);

            switch (logLevel)
            {
                case LogLevel.Trace:
                    seriLogger.Verbose(logMessage);
                    break;
                case LogLevel.Debug:
                    seriLogger.Debug(logMessage);
                    break;
                case LogLevel.Information:
                    seriLogger.Information(logMessage);
                    break;
                case LogLevel.Warning:
                    seriLogger.Warning(logMessage);
                    break;
                case LogLevel.Error:
                    seriLogger.Error(logMessage);
                    break;
                case LogLevel.Critical:
                    seriLogger.Fatal(logMessage);
                    break;
                default:
                    seriLogger.Error($"Log level {logLevel} not recognized.");
                    break;
            }
        }
    }
}

