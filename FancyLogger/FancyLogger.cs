using Microsoft.Extensions.Logging;
using Refit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using XamarinFiles.FancyLogger.Helpers;
using XamarinFiles.FancyLogger.Options;
using XamarinFiles.PdHelpers.Refit.Enums;
using XamarinFiles.PdHelpers.Refit.Models;
using static System.Net.HttpStatusCode;
using static System.Net.Sockets.SocketError;
using static System.Net.WebExceptionStatus;
using static XamarinFiles.FancyLogger.Constants.Characters;
using static XamarinFiles.PdHelpers.Refit.Enums.ProblemLevel;

// Ignore specialized exception processing since purpose is structural logging
#pragma warning disable CA1031

// Using complex IFormatProvider misses whole point of FL for structural logging
#pragma warning disable CA1305

// Using LoggerMessage delegates misses whole point of FL for structural logging
#pragma warning disable CA1848

namespace XamarinFiles.FancyLogger
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class FancyLogger : IFancyLogger
    {
        #region Fields - Services

        private readonly ILogger _logger;

        private readonly Serializer _serializer;

        #endregion

        #region Fields - Options

        public static readonly FancyLoggerOptions
            DefaultLoggerOptions = new();

        public static readonly JsonSerializerOptions
            DefaultReadOptions = new()
            {
                AllowTrailingCommas = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
                PropertyNameCaseInsensitive = true
            };

        public static readonly JsonSerializerOptions
            DefaultWriteJsonOptions = new()
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            };

        #endregion

        // TODO Test with other log destinations after finish porting test code
        #region Constructor

        // TODO Add ctor to tie default (non-hosted) logger directly to type

        // For static classes or multiple logger instances with custom prefixes
        public FancyLogger(ILogger? logger = null,
            // Common overrides => create direct paths to avoid options object
            string? allLinesPrefix = null,
            int? allLinesPadLength = null,

            // All overrides => create path to override all options at once
            FancyLoggerOptions? loggerOptions = null,

            // Serializer overrides  => System.Text.Json options for LogObject
            JsonSerializerOptions? readJsonOptions = null,
            JsonSerializerOptions? writeJsonOptions = null)
        {
            try
            {
                loggerOptions ??= DefaultLoggerOptions;
                if (allLinesPrefix is not null)
                {
                    loggerOptions.AllLines.PrefixString = allLinesPrefix;
                }
                if (allLinesPadLength is not null)
                {
                    loggerOptions.AllLines.PadLength = (int) allLinesPadLength;
                }
                LoggerOptions = loggerOptions;

                ReadJsonOptions = readJsonOptions ?? DefaultReadOptions;
                WriteJsonOptions = writeJsonOptions ?? DefaultWriteJsonOptions;

                // Map Shorthand Properties
                AllLinesPrefixString = LoggerOptions.AllLines.PrefixString;
                AllLinesPadLength = LoggerOptions.AllLines.PadLength;
                AllLinesPadString = LoggerOptions.AllLines.PadString;

                LongDividerLinesPadLength = LoggerOptions.LongDividerLines.PadLength;
                LongDividerLinesPadString = LoggerOptions.LongDividerLines.PadString;

                ShortDividerLinesPadLength = LoggerOptions.ShortDividerLines.PadLength;
                ShortDividerLinesPadString = LoggerOptions.ShortDividerLines.PadString;

                SectionLinesPadLength = LoggerOptions.SectionLines.PadLength;
                SectionLinesPadString = LoggerOptions.SectionLines.PadString;

                SubsectionLinesPadLength = LoggerOptions.SubsectionLines.PadLength;
                SubsectionLinesPadString = LoggerOptions.SubsectionLines.PadString;

                HeaderLinesPadLength = LoggerOptions.HeaderLines.PadLength;
                HeaderLinesPadString = LoggerOptions.HeaderLines.PadString;

                FooterLinesPadLength = LoggerOptions.FooterLines.PadLength;
                FooterLinesPadString = LoggerOptions.FooterLines.PadString;

                _logger = logger ?? LoggerCreator.CreateLogger(
                    AllLinesPrefixString, AllLinesPadLength,
                    AllLinesPadString);
                _serializer = new Serializer(ReadJsonOptions, WriteJsonOptions);
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"EXCEPTION: {exception.Message}");

                throw;
            }
        }

        #endregion

        #region Public Properties

        // Constructor Parameters

        public FancyLoggerOptions LoggerOptions { get; }

        public JsonSerializerOptions ReadJsonOptions { get; }

        public JsonSerializerOptions WriteJsonOptions { get; }

        // FancyLoggerOptions Values

        public string AllLinesPrefixString { get; private set; }
        public int AllLinesPadLength { get; private set; }
        public string AllLinesPadString { get; private set; }

        public int LongDividerLinesPadLength { get; private set; }
        public string LongDividerLinesPadString { get; private set; }

        public int ShortDividerLinesPadLength { get; private set; }
        public string ShortDividerLinesPadString { get; private set; }

        public int SectionLinesPadLength { get; private set; }
        public string SectionLinesPadString { get; private set; }

        public int SubsectionLinesPadLength { get; private set; }
        public string SubsectionLinesPadString { get; private set; }

        public int HeaderLinesPadLength { get; private set; }
        public string HeaderLinesPadString { get; private set; }

        public int FooterLinesPadLength { get; private set; }
        public string FooterLinesPadString { get; private set; }

        #endregion

        #region Public Methods - Exceptions

        // Exception Router

        // TODO Add toggle for LogError vs LogWarning
        public void LogException(Exception exception)
        {
            switch (exception)
            {
                case ValidationApiException validationException:
                    LogApiException(validationException);

                    break;
                case ApiException apiException:
                    LogApiException(apiException);

                    break;
                case HttpRequestException httpRequestException:
                    LogHttpRequestException(httpRequestException);

                    break;
                case JsonException jsonException:
                    LogJsonException(jsonException);

                    break;
                default:
                    LogGeneralException(exception);

                    break;
            }
        }

        // Direct Access

        // TODO Log other details of ApiException
        public void LogApiException(ApiException? apiException)
        {
            if (apiException == null)
                return;

            var request = apiException.RequestMessage;

            LogCommonException(apiException, "API EXCEPTION");

            LogWarning(
                $"OPERATION:  {request.Method} - {apiException.Uri}" + NewLine);

            if (apiException.Content is null)
                return;

            // TODO Document expectation of ProblemDetails or handle alternative
            LogObject<ProblemDetails>(apiException.Content);
        }

        // TODO Handle aggregate and nested exceptions - see old FL library
        // TODO Handle stack traces - see old FL library
        public void LogCommonException(Exception? exception, string outerLabel,
            string innerLabel = "INNER EXCEPTION")
        {
            if (exception is null)
                return;

            LogWarning($"{outerLabel}:  {exception.Message}{NewLine}");

            var innerExceptionMessage = exception.InnerException?.Message;

            if (string.IsNullOrWhiteSpace(innerExceptionMessage))
                return;

            LogWarning($"{Indent}{innerLabel}:  {innerExceptionMessage}{NewLine}");
        }

        public void LogGeneralException(Exception exception)
        {
            LogCommonException(exception, "EXCEPTION");
        }

        [SuppressMessage("ReSharper", "MergeIntoPattern")]
        public void LogHttpRequestException(HttpRequestException? requestException)
        {
            if (requestException == null)
                return;

            const string outerExceptionLabel = "HTTP REQUEST EXCEPTION";
            // TODO Use when > .NET Standard 2.0 for Xamarin.Forms
            //var outerStatusCode = requestException.StatusCode;

            string innerExceptionLabel;
            HttpStatusCode? innerStatusCode = null;

            // TODO Add more networking error conditions
            switch (requestException.InnerException)
            {
                case SocketException socketException
                    when socketException.SocketErrorCode == ConnectionRefused:

                    innerExceptionLabel =
                        "SOCKET EXCEPTION - ConnectionRefused";

                    innerStatusCode = ServiceUnavailable;

                    break;
                case WebException webException
                    when webException.Status == NameResolutionFailure:

                    innerExceptionLabel =
                        "WEB EXCEPTION - NameResolutionFailure";

                    innerStatusCode = ServiceUnavailable;

                    break;

                default:
                    innerExceptionLabel = "INNER EXCEPTION";

                    break;
            }

            innerExceptionLabel += $"{innerStatusCode?.ToString() ?? ""}";

            LogCommonException(requestException, outerExceptionLabel,
                innerExceptionLabel);
        }

        public void LogJsonException(JsonException? jsonException)
        {
            if (jsonException is null)
                return;

            LogCommonException(jsonException, "JSON EXCEPTION");

            LogWarning($"LINE:  {jsonException.LineNumber}"
                       + $"{Indent}-{Indent}{jsonException.BytePositionInLine}");
            LogWarning($"PATH:  {jsonException.Path}");
        }

        #endregion

        #region Public Methods - General Logging

        public void LogDebug(string format, bool addIndent = false,
            bool newLineAfter = false, params object[]? args)
        {
            try
            {
                var messagePrefix =
                    // Difference in prefix length: "Debug" vs "Information"
                    new string(' ', 6) +
                    AddIndent(addIndent);

                var message = messagePrefix
                    // TODO Handle format + args with separate {}s like Dictionary
                    + (args is null || args.Length == 0
                        ? format
                        : string.Format(format, args));

                if (newLineAfter)
                    message += NewLine;

                _logger.LogDebug("{Message}", message);

            }
            catch (Exception exception)
            {
                LogException(exception);
            }
        }

        public void LogError(string format, bool addIndent = true,
            bool newLineAfter = true, params object[] args)
        {
            var message =
                // Difference in prefix length: "Error" vs "Information"
                new string(' ', 6)
                + AddIndent(addIndent) + string.Format(format, args);
            if (newLineAfter)
                message += NewLine;

            _logger.LogError("{Message}", message);
        }

        public void LogErrorOrWarning(ProblemLevel problemLevel,
            string format, bool addIndent = true, bool newLineAfter = true,
            params object[] args)
        {
            switch (problemLevel)
            {
                case Error:
                    LogError(format, addIndent, newLineAfter, args);

                    break;
                case Warning:
                    LogWarning(format, addIndent, newLineAfter, args);

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(problemLevel),
                        problemLevel, null);
            }
        }

        public void LogInfo(string format, bool addIndent = true,
            bool newLineAfter = true, params object[] args)
        {
            var message =
                AddIndent(addIndent) + string.Format(format, args);
            if (newLineAfter)
                message += NewLine;

            _logger.LogInformation("{Message}", message);
        }

        public void LogObject<T>(object? obj, bool ignore = false,
            bool keepNulls = false, string? label = null, bool addIndent = true,
            bool newLineAfter = true)
        {
            if (ignore || obj is null)
                return;

            try
            {
                var (formattedJson, problemReport) =
                    _serializer.ToJson<T>(obj, keepNulls);

                if (problemReport != null)
                    LogProblemReport(problemReport);

                if (string.IsNullOrWhiteSpace(formattedJson))
                    return;

                LogDebug(AddIndent(addIndent) + $"{label}:" + NewLine
                    + formattedJson, newLineAfter: newLineAfter);
            }
            catch (Exception exception)
            {
                LogException(exception);
            }
        }

        public void LogScalar(string label, string? value, bool addIndent = true,
            bool newLineAfter = true)
        {
            // TODO Add flag to skip if null?
            value ??= "NULL";

            var message =
                // Difference in prefix length: "Debug" vs "Information"
                new string(' ', 6) +
                AddIndent(addIndent) + $"{label}{Indent}={Indent}{value}";
            if (newLineAfter)
                message += NewLine;

            _logger.LogDebug("{Message}", message);
        }

        public void LogTrace(string format, bool addIndent = false,
            bool newLineAfter = false, params object[]? args)
        {
            var messagePrefix =
                // Difference in prefix length: "Trace" vs "Information"
                new string(' ', 6) + AddIndent(addIndent);

            var message = messagePrefix
                // TODO Handle format + args with separate {}s like Dictionary
                + (args is null || args.Length == 0
                    ? format
                    : string.Format(format, args));

            if (newLineAfter)
                message += NewLine;

            _logger.LogTrace("{Message}", message);
        }

        public void LogWarning(string format, bool addIndent = true,
            bool newLineAfter = true, params object[] args)
        {
            var message =
                // Difference in prefix length: "Warning" vs "Information"
                new string(' ', 4)
                + AddIndent(addIndent) + string.Format(format, args);
            if (newLineAfter)
                message += NewLine;

            _logger.LogWarning("{Message}", message);
        }

        #endregion

        #region Public Methods - Specialized Logging

        // TODO Fix column alignments
        public void LogProblemDetails(ProblemDetails? problemDetails,
            ProblemLevel problemLevel)
        {
            if (problemDetails == null)
                return;

            try
            {
                LogErrorOrWarning(problemLevel, "PROBLEMDETAILS",
                    newLineAfter: false);
                LogErrorOrWarning(problemLevel,
                    $"Status: {Indent}{Indent}{problemDetails.Status}",
                    newLineAfter: false);
                LogErrorOrWarning(problemLevel,
                    $"Title: {Indent}{Indent}{Indent}'{problemDetails.Title}'",
                    newLineAfter: false);
                LogErrorOrWarning(problemLevel,
                    $"Detail: {Indent}{Indent}'{problemDetails.Detail}'",
                    newLineAfter: false);
                LogErrorOrWarning(problemLevel,
                    $"Instance URL: {Indent}'{problemDetails.Instance}'",
                    newLineAfter: false);
                LogErrorOrWarning(problemLevel,
                    $"Type Info: {Indent}{Indent}'{problemDetails.Type}'",
                    addIndent: true);

                LogObject<Dictionary<string, object>>(
                    problemDetails.Extensions, label: "Extensions Dictionary",
                    newLineAfter: true);

                LogObject<Dictionary<string, string[]>>(
                    problemDetails.Errors, label: "Errors Dictionary",
                    newLineAfter: true);
            }
            catch (Exception exception)
            {
                LogException(exception);
            }
        }

        // TODO Fix column alignments
        // TODO Workaround last field in block being null
        public void LogProblemReport(ProblemReport? problemReport)
        {
            if (problemReport == null)
                return;

            var problemLevel = problemReport.ProblemLevelEnum;

            try
            {
                LogErrorOrWarning(problemLevel,
                    $"Problem Variant: {Indent}{problemReport.ProblemVariant}",
                    newLineAfter: false);
                LogErrorOrWarning(problemLevel,
                    $"Problem Level: {Indent}{Indent}{problemReport.ProblemLevel}",
                    newLineAfter: true);

                var sourceDetails = problemReport.SourceDetails;
                if (sourceDetails is not null)
                {
                    LogErrorOrWarning(problemLevel,
                        $"Source Assembly: {Indent}'{sourceDetails.AssemblyName}'",
                        newLineAfter: false);
                    LogErrorOrWarning(problemLevel,
                        $"Source Component: {Indent}'{sourceDetails.ComponentName}'",
                        newLineAfter: false);
                    LogErrorOrWarning(problemLevel,
                        $"Source Operation: {Indent}'{sourceDetails.OperationName}'",
                        newLineAfter: true);
                }

                var exceptionDetails = problemReport.ExceptionDetails;
                if (exceptionDetails is not null)
                {
                    LogErrorOrWarning(problemLevel,
                        $"Exception Assembly: {Indent}'{exceptionDetails.Assembly}'",
                        newLineAfter: false);
                    LogErrorOrWarning(problemLevel,
                        $"Exception Method: {Indent}'{exceptionDetails.Method}'",
                        newLineAfter: false);
                    LogObject<ExceptionMessages>(exceptionDetails.Messages,
                        label: "Exception Messages",
                        newLineAfter: true);
                }

                var requestDetails = problemReport.RequestDetails;
                if (requestDetails is not null)
                {
                    LogErrorOrWarning(problemLevel,
                        $"Request Method: {Indent}'{requestDetails.Method}'",
                        newLineAfter: false);
                    LogErrorOrWarning(problemLevel,
                        $"Request Uri: {Indent}{Indent}'{requestDetails.Uri}'",
                        newLineAfter: false);
                    LogErrorOrWarning(problemLevel,
                        $"Request Controller: {Indent}'{requestDetails.Controller}'",
                        newLineAfter: false);
                    LogErrorOrWarning(problemLevel,
                        $"Request Resource: {Indent}'{requestDetails.Resource}'",
                        newLineAfter: true);
                }

                var responseDetails = problemReport.ResponseDetails;
                if (responseDetails is not null)
                {
                    LogErrorOrWarning(problemLevel,
                        $"Status: {Indent}{Indent}{responseDetails.StatusCodeInt}"
                        + $" - {responseDetails.StatusTitle}",
                        newLineAfter: false);
                    LogErrorOrWarning(problemLevel,
                        $"Title: {Indent}{Indent}{Indent}'{responseDetails.ProblemSummary}'",
                        newLineAfter: false);
                    LogErrorOrWarning(problemLevel,
                        $"Detail: {Indent}{Indent}'{responseDetails.ProblemExplanation}'",
                        newLineAfter: false);
                    LogErrorOrWarning(problemLevel,
                        $"Instance URL: {Indent}'{responseDetails.InstanceUri}'",
                        newLineAfter: false);
                    LogErrorOrWarning(problemLevel,
                        $"Type Info: {Indent}{Indent}'{responseDetails.StatusReference}'",
                        newLineAfter: true);
                }

                LogObject<OtherMessages>(problemReport.OtherMessages,
                    label: "Messages",
                    newLineAfter: true);
                // TODO
                //LogObject<Dictionary<string, object>>(
                //    problemReport.Extensions, label: "Extensions Dictionary",
                //    newLineAfter: false);
                //LogObject<Dictionary<string, string[]>>(
                //    problemReport.OtherErrors, label: "OtherErrors Dictionary",
                //    newLineAfter: true);
            }
            catch (Exception exception)
            {
                LogException(exception);
            }
        }

        #endregion

        #region Public Methods - Structral Logging

        // Horizontal Lines

        public void LogLongDividerLine()
        {
            var dividerLine =
                "".PadRight(LongDividerLinesPadLength, LongDividerLinesPadString);

            LogInfo(dividerLine, newLineAfter: true);
        }

        public void LogShortDividerLine()
        {
            var dividerLine =
                "".PadRight(ShortDividerLinesPadLength, ShortDividerLinesPadString);

            LogInfo(dividerLine, newLineAfter: true);
        }

        // Section and Subsection

        public void LogSection(string format, params object[] args)
        {
            var message = string.Format(format, args).Trim() + " ";
            var paddedMessage =
                message.PadRight(SectionLinesPadLength, SectionLinesPadString);

            LogInfo(paddedMessage, newLineAfter: true);
        }

        public void LogSubsection(string format, params object[] args)
        {
            var message = string.Format(format, args).Trim() + " ";
            var paddedMessage =
                message.PadRight(SubsectionLinesPadLength,
                    SubsectionLinesPadString);

            LogInfo(paddedMessage, newLineAfter: true);
        }

        // Header and Footer

        public void LogHeader(string format, bool addStart = false,
            params object[] args)
        {
            if (addStart)
                format += " - Start";
            var message = string.Format(format, args).Trim() + " ";
            var paddedMessage =
                message.PadRight(HeaderLinesPadLength, HeaderLinesPadString);

            LogInfo(paddedMessage, newLineAfter: true);
        }

        public void LogFooter(string format, bool addEnd = false,
            params object[] args)
        {
            if (addEnd)
                format += " - End";
            var message = string.Format(format, args).Trim() + " ";
            var paddedMessage =
                message.PadRight(FooterLinesPadLength, FooterLinesPadString);

            LogInfo(paddedMessage, newLineAfter: true);
        }

        #endregion

        #region Private Methods

        // TODO Go back to allowing multiple levels like the old FL library
        private static string AddIndent(bool addExtraIndent)
        {
            // Optional ident for nesting
            return addExtraIndent ? Indent : "";
        }

        #endregion
    }
}
