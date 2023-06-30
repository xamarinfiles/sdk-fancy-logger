using Microsoft.Extensions.Logging;
using static XamarinFiles.FancyLogger.Characters;

namespace XamarinFiles.FancyLogger
{
    internal static class LoggerCreator
    {
        #region Public

        internal static ILogger CreateLogger<T>()
        {
            var loggerFactory = CreateLoggerFactory();
            var logger = loggerFactory.CreateLogger<T>();

            logger.LogInformation("Logger created" + NewLine);

            return logger;
        }

        internal static ILogger CreateLogger(string categoryName)
        {
            var loggerFactory = CreateLoggerFactory();
            var logger = loggerFactory.CreateLogger(categoryName);

            logger.LogInformation("Logger created" + NewLine);

            return logger;
        }

        #endregion

        #region Private

        private static ILoggerFactory CreateLoggerFactory()
        {
            // Create a logger factory
            var loggerFactory = LoggerFactory.Create(
                builder => builder
//#if DEBUG
                    .AddDebug()
                    .SetMinimumLevel(LogLevel.Trace)
//#else
//                    .SetMinimumLevel(LogLevel.Information)
//#endif
            );

            return loggerFactory;
        }

        #endregion
    }
}
