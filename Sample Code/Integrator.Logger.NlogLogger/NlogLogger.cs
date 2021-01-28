using Microsoft.Extensions.Logging;
using System;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using MSLogging = Microsoft.Extensions.Logging;

namespace Integrator.Logger.NlogLogger
{
    public class NlogLogger : ILogger
    {
        private readonly NLog.Logger logger;

        public NlogLogger()
        {
            Configure();
            logger = NLog.LogManager.GetCurrentClassLogger();
        }
        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(MSLogging.LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    return logger.IsEnabled(NLog.LogLevel.Trace);
                case LogLevel.Debug:
                    return logger.IsEnabled(NLog.LogLevel.Debug);
                case LogLevel.Information:
                    return logger.IsEnabled(NLog.LogLevel.Info);
                case LogLevel.Warning:
                    return logger.IsEnabled(NLog.LogLevel.Warn);
                case LogLevel.Error:
                    return logger.IsEnabled(NLog.LogLevel.Error);
                case LogLevel.Critical:
                    return logger.IsEnabled(NLog.LogLevel.Fatal);
                default:
                    return false;
            }

        }

        public void Log<TState>(MSLogging.LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            string logMessage = formatter.Invoke(state, exception);

            switch (logLevel)
            {
                case LogLevel.Trace:
                    logger.Info(logMessage);
                    break;
                case LogLevel.Debug:
                    logger.Debug(logMessage);
                    break;
                case LogLevel.Information:
                    logger.Info(logMessage);
                    break;
                case LogLevel.Warning:
                    logger.Warn(logMessage);
                    break;
                case LogLevel.Error:
                    logger.Error(logMessage);
                    break;
                case LogLevel.Critical:
                    logger.Fatal(logMessage);
                    break;
                default:
                    logger.Error($"Log level {logLevel} not recognized.");
                    break;
            }
        }

        public void Configure()
        {
            var config = new NLog.Config.LoggingConfiguration();

            // Targets where to log to: File and Console
            var logfile = new NLog.Targets.FileTarget("logfile") { FileName = "NlogLog.txt" };

            // Rules for mapping loggers to targets            
            config.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, logfile);

            // Apply config           
            NLog.LogManager.Configuration = config;
        }
    }
}
