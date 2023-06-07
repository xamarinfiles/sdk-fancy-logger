using Microsoft.Extensions.Logging;
using Refit;
using System;
using System.Net.Http;
using System.Text.Json;

namespace XamarinFiles.FancyLogger
{
    public interface IFancyLoggerService
    {
        #region Properties

        char HeaderChar { get; }

        int HeaderLength { get; }

        string LoggerPrefix { get; }

        #endregion

        #region Methods

        void LogDebug(string format, bool addIndent = false,
            bool newLineAfter = false, params object[] args);

        void LogError(string format, params object[] args);

        void LogException(Exception exception);

        void LogHeader(string format, params object[] args);

        void LogHorizontalLine();

        void LogInfo(string format, bool addIndent = false,
            bool newLineAfter = true, params object[] args);

        void LogObject<T>(object obj, bool ignore = false,
            bool keepNulls = false, string label = null,
            bool newLineAfter = true);

        void LogProblemDetails(ProblemDetails problemDetails);

        void LogTrace(string format, bool addIndent = false,
            bool newLineAfter = false, params object[] args);

        void LogValue(string label, string value,  bool addIndent = false,
            bool newLineAfter = false);

        void LogWarning(string format,  bool addIndent = false,
            bool newLineAfter = true, params object[] args);

        #endregion

        #region Deprecated Methods

        [Obsolete("This method is obsolete. Call LogException instead.",
            error: false)]
        void LogExceptionRouter(Exception exception);

        [Obsolete("This method is obsolete. Call LogObject instead.",
            error: false)]
        void LogObjectAsJson<T>(object obj, bool ignore = false,
            bool keepNulls = false, bool newLineAfter = true);

        #endregion
    }
}