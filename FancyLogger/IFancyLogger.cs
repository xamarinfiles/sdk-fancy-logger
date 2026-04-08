using Refit;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Text.Json;
using XamarinFiles.FancyLogger.Options;
using XamarinFiles.PdHelpers.Refit.Enums;
using XamarinFiles.PdHelpers.Refit.Models;

namespace XamarinFiles.FancyLogger
{
    [SuppressMessage("ReSharper", "UnusedMemberInSuper.Global")]
    public interface IFancyLogger
    {
        #region Options

        FancyLoggerOptions LoggerOptions { get; }

        JsonSerializerOptions ReadJsonOptions { get; }

        JavaScriptEncoder WriteJavaScriptEncoder { get; }

        JsonSerializerOptions WriteJsonOptions { get; }

        #endregion

        #region Exceptions

        // Exception Router

        void LogException(Exception? exception);

        // Direct Access

        void LogApiException(ApiException? apiException);

        void LogCommonException(Exception? exception, string outerLabel,
            string innerLabel = "INNER EXCEPTION");

        void LogGeneralException(Exception? exception);

        void LogHttpRequestException(HttpRequestException? requestException);

        void LogJsonException(JsonException? jsonException);

        #endregion

        #region General Logging

        void LogDebug(string format, bool addIndent = false,
            bool newLineAfter = false, params object[] args);

        void LogError(string format, bool addIndent = false,
            bool newLineAfter = true, params object[] args);

        void LogErrorOrWarning(ProblemLevel problemLevel, string format,
            bool addIndent = false, bool newLineAfter = true,
            params object[] args);

        void LogInfo(string format, bool addIndent = false,
            bool newLineAfter = true, params object[] args);

        void LogObject<T>(object? obj, bool ignore = false,
            bool keepNulls = false, string? label = null, bool addIndent = false,
            bool newLineAfter = true);

        void LogScalar(string label, string? value,  bool addIndent = false,
            bool newLineAfter = false);

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        void LogTrace(string format, bool addIndent = false,
            bool newLineAfter = false, params object[] args);

        void LogWarning(string format,  bool addIndent = false,
            bool newLineAfter = true, params object[] args);

        #endregion

        #region Specialized Logging

        void LogProblemDetails(ProblemDetails? problemDetails,
            ProblemLevel problemLevel);

        void LogProblemReport(ProblemReport? problemReport);

        #endregion

        #region Structral Logging

        void LogLongDividerLine();

        void LogShortDividerLine();

        void LogSection(string format, params object[] args);

        void LogSubsection(string format, params object[] args);

        void LogHeader(string format, bool addStart = false, params object[] args);

        void LogFooter(string format, bool addEnd = false, params object[] args);

        #endregion
    }
}
