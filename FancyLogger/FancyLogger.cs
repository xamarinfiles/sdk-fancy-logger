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
using static XamarinFiles.PdHelpers.Refit.Enums.ErrorOrWarning;

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

                _logger.LogDebug("{message}", message);

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

            _logger.LogError("{message}", message);
        }

        public void LogErrorOrWarning(ErrorOrWarning errorOrWarning,
            string format, bool addIndent = true, bool newLineAfter = true,
            params object[] args)
        {
            switch (errorOrWarning)
            {
                case Error:
                    LogError(format, addIndent, newLineAfter, args);

                    break;
                case Warning:
                    LogWarning(format, addIndent, newLineAfter, args);

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(errorOrWarning),
                        errorOrWarning, null);
            }
        }

        public void LogInfo(string format, bool addIndent = true,
            bool newLineAfter = true, params object[] args)
        {
            var message =
                AddIndent(addIndent) + string.Format(format, args);
            if (newLineAfter)
                message += NewLine;

            _logger.LogInformation("{message}", message);
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
                    LogProblemReport(problemReport, Error);

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

            _logger.LogDebug("{message}", message);
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

            _logger.LogTrace("{message}", message);
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

            _logger.LogWarning("{message}", message);
        }

        #endregion

        #region Public Methods - Specialized Logging

        // TODO Fix column alignments
        public void LogProblemDetails(ProblemDetails? problemDetails,
            ErrorOrWarning errorOrWarning)
        {
            if (problemDetails == null)
                return;

            try
            {
                LogErrorOrWarning(errorOrWarning, "PROBLEMDETAILS",
                    newLineAfter:false);
                LogErrorOrWarning(errorOrWarning,
                    $"Status Code: {Indent}{problemDetails.Status}",
                    newLineAfter:false);
                LogErrorOrWarning(errorOrWarning,
                    $"Title: {Indent}'{problemDetails.Title}'",
                    newLineAfter:false);
                LogErrorOrWarning(errorOrWarning,
                    $"Detail: {Indent}'{problemDetails.Detail}'",
                    newLineAfter:false);
                LogErrorOrWarning(errorOrWarning,
                    $"Instance URL: {Indent}'{problemDetails.Instance}'",
                    newLineAfter: true);

                LogDebug($"Type Info: {Indent}'{problemDetails.Type}'",
                    addIndent: true);
                LogObject<Dictionary<string, object>>(
                    problemDetails.Extensions, label: "Extensions Dictionary",
                    newLineAfter: false);
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
        public void LogProblemReport(ProblemReport? problemReport,
            ErrorOrWarning errorOrWarning)
        {
            if (problemReport == null)
                return;

            try
            {
                LogErrorOrWarning(errorOrWarning,
                    $"Variant: {Indent}'{problemReport.DetailsVariantName}'",
                    newLineAfter:false);
                LogErrorOrWarning(errorOrWarning,
                    $"Error or Warning: '{problemReport.ErrorOrWarning}'",
                    newLineAfter:true);

                var appStateDetails = problemReport.AppStateDetails;
                LogErrorOrWarning(errorOrWarning,
                    $"App Location: {Indent}'{appStateDetails?.Location}'",
                    newLineAfter: false);
                LogErrorOrWarning(errorOrWarning,
                    $"App Operation: {Indent}'{appStateDetails?.Operation}'",
                    newLineAfter: true);

                var requestDetails = problemReport.RequestDetails;
                LogErrorOrWarning(errorOrWarning,
                    $"Request Method: {Indent}'{requestDetails?.HttpMethodName}'",
                    newLineAfter: false);
                LogErrorOrWarning(errorOrWarning,
                    $"Request Resource: {Indent}'{requestDetails?.ResourceName}'",
                    newLineAfter: false);
                LogErrorOrWarning(errorOrWarning,
                    $"Request Uri: {Indent}'{requestDetails?.UriString}'",
                    newLineAfter: true);

                var responseDetails = problemReport.ResponseDetails;
                LogErrorOrWarning(errorOrWarning,
                    $"Status Code: {Indent}{responseDetails.StatusCodeInt}"
                    + $" - {responseDetails.StatusTitle}",
                    newLineAfter:false);
                LogErrorOrWarning(errorOrWarning,
                    $"Title: {Indent}'{responseDetails.ProblemSummary}'",
                    newLineAfter:false);
                LogErrorOrWarning(errorOrWarning,
                    $"Detail: {Indent}'{responseDetails.ProblemExplanation}'",
                    newLineAfter:false);
                LogErrorOrWarning(errorOrWarning,
                    $"Instance URL: {Indent}'{responseDetails.InstanceUri}'",
                    newLineAfter: false);
                LogErrorOrWarning(errorOrWarning,
                    $"Type Info: {Indent}'{responseDetails.StatusReference}'",
                    newLineAfter: false);

                LogObject<Messages>(problemReport.ImportantMessages,
                    label: "Messages",
                    newLineAfter: false);
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
