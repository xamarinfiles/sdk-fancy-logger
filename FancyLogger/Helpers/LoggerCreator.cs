using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using static XamarinFiles.FancyLogger.Constants.Characters;

// Using LoggerMessage delegates misses whole point of FL for structural logging
#pragma warning disable CA1848

namespace XamarinFiles.FancyLogger.Helpers
{
    internal static class LoggerCreator
    {
        #region Public

        // TODO See CreateLogger(ILoggerFactory, Type) in Microsoft Learn
        // For instantiated classes with default prefix of simple class name
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        internal static ILogger CreateLogger<T>() where T : class
        {
            var loggerFactory = CreateLoggerFactory();

            var logger = loggerFactory.CreateLogger<T>();
            logger.LogInformation("{Indent}Logger created{NewLine}",
                Indent, NewLine);

            loggerFactory.Dispose();

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
            logger.LogInformation("{Indent}Logger created{NewLine}",
                Indent, NewLine);

            loggerFactory.Dispose();

            return logger;
        }

        #endregion

        #region Private

        private static ILoggerFactory CreateLoggerFactory()
        {
            var loggerFactory = LoggerFactory.Create(
                builder => builder
                    .AddDebug()
                    .SetMinimumLevel(LogLevel.Trace)
            );

            return loggerFactory;
        }

        #endregion
    }
}
