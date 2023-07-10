using Microsoft.Extensions.Logging;
using static XamarinFiles.FancyLogger.Helpers.Characters;

namespace XamarinFiles.FancyLogger.Helpers
{
    internal static class LoggerCreator
    {
        #region Public

        // TODO See CreateLogger(ILoggerFactory, Type) in Microsoft Learn
        // For instantiated classes with default prefix of simple class name
        internal static ILogger CreateLogger<T>() where T : class
        {
            var loggerFactory = CreateLoggerFactory();
            var logger = loggerFactory.CreateLogger<T>();

            logger.LogInformation("Logger created" + NewLine);

            return logger;
        }

        // For static classes or multiple logger instances with custom prefixes
        internal static ILogger CreateLogger(string categoryName,
            int prefixPadLength, string prefixPadString)
        {
            // TODO Instead create padding to add after colon from CreateLogger
            var paddedCategoryName =
                categoryName.PadRight(prefixPadLength, prefixPadString);

            var loggerFactory = CreateLoggerFactory();
            var logger = loggerFactory.CreateLogger(paddedCategoryName);

            logger.LogInformation(Indent + "Logger created" + NewLine);

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
