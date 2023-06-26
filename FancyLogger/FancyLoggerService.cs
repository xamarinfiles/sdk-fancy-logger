using Microsoft.Extensions.Logging;
using Refit;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Net.HttpStatusCode;
using static System.Net.Sockets.SocketError;
using static System.Net.WebExceptionStatus;
using static XamarinFiles.FancyLogger.Characters;
using static XamarinFiles.PdHelpers.Shared.StatusCodeDetails;

namespace XamarinFiles.FancyLogger
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class FancyLoggerService : IFancyLoggerService
    {
        #region Fields - Defaults

        private const int DefaultHeaderPaddingLength = 70;

        private const char DefaultHeaderPaddingChar = '#';

        private const string DefaultLoggerPrefix = "LOG";

        private const int DefaultSubheaderPaddingLength = 60;

        private const char DefaultSubheaderPaddingChar = '=';

        #endregion

        #region Fields - Services

        private readonly ILogger _logger;

        private readonly Serializer _serializer;

        #endregion

        #region Fields - JsonSerializerOptions

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
                WriteIndented = true
            };

        #endregion

        #region Constructor

        // TODO Move parameters to options object
        // TODO Test with other log destinations after finish porting code
        public FancyLoggerService(ILogger logger = null,
            string loggerPrefix = DefaultLoggerPrefix,
            int headerPaddedLength = DefaultHeaderPaddingLength,
            char headerPaddingChar = DefaultHeaderPaddingChar,
            int subheaderPaddedLength = DefaultSubheaderPaddingLength,
            char subheaderPaddingChar = DefaultSubheaderPaddingChar,
            JsonSerializerOptions readJsonOptions = null,
            JsonSerializerOptions writeJsonOptions = null)
        {
            LoggerPrefix = loggerPrefix;
            HeaderLength = headerPaddedLength;
            HeaderChar = headerPaddingChar;
            SubheaderLength = subheaderPaddedLength;
            SubheaderChar = subheaderPaddingChar;
            ReadJsonOptions = readJsonOptions ?? DefaultReadOptions;
            WriteJsonOptions = writeJsonOptions ?? DefaultWriteJsonOptions;

            _logger = logger ?? LoggerCreator.CreateLogger(LoggerPrefix);
            _serializer = new Serializer(ReadJsonOptions, WriteJsonOptions);
        }

        #endregion

        #region Public Properties

        public char HeaderChar { get; }

        public int HeaderLength { get; }

        public string LoggerPrefix { get; }

        public JsonSerializerOptions ReadJsonOptions { get; }

        public char SubheaderChar { get; }

        public int SubheaderLength { get; }

        public JsonSerializerOptions WriteJsonOptions { get; }

        #endregion

        #region Public Methods

        public void LogDebug(string format, bool addIndent = false,
            bool newLineAfter = false, params object[] args)
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

        public void LogError(string format, params object[] args)
        {
            var message = string.Format(format + NewLine, args);

            _logger.LogError("{message}", message);
        }

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

        public void LogFooter(string format, bool addEnd = false,
            params object[] args)
        {
            if (addEnd)
                format += " - End";
            var message = string.Format(format, args).Trim() + " ";
            var paddedMessage =
                message.PadRight(HeaderLength, HeaderChar);

            LogInfo(paddedMessage, newLineAfter: true);
        }

        public void LogHeader(string format, bool addStart = false,
            params object[] args)
        {
            if (addStart)
                format += " - Start";
            var message = string.Format(format, args).Trim() + " ";
            var paddedMessage =
                message.PadRight(HeaderLength, HeaderChar);

            LogInfo(paddedMessage, newLineAfter: true);
        }

        public void LogHorizontalLine()
        {
            LogHeader(new string(HeaderChar, HeaderLength));
        }

        public void LogInfo(string format, bool addIndent = false,
            bool newLineAfter = true, params object[] args)
        {
            var message =
                AddIndent(addIndent) + string.Format(format, args);
            if (newLineAfter)
                message += NewLine;

            _logger.LogInformation("{message}", message);
        }

        public void LogObject<T>(object obj, bool ignore = false,
            bool keepNulls = false, string label = null,
            bool newLineAfter = true)
        {
            if (ignore || obj is null)
                return;

            try
            {
                var (formattedJson, problemDetails) =
                    _serializer.ToJson<T>(obj, keepNulls);

                if (problemDetails != null)
                    LogProblemDetails(problemDetails);

                if (string.IsNullOrWhiteSpace(formattedJson))
                    return;

                LogTrace($"{label}:" + NewLine + formattedJson,
                    newLineAfter: newLineAfter);
            }
            catch (Exception exception)
            {
                LogException(exception);
            }
        }

        // TODO Add toggle for LogError vs LogWarning
        public void LogProblemDetails(ProblemDetails problemDetails)
        {
            if (problemDetails == null)
                return;

            try
            {
                LogWarning($"PROBLEMDETAILS", newLineAfter:false);
                LogWarning($"Title: '{problemDetails.Title}'",
                    newLineAfter:false);
                LogWarning($"Detail: '{problemDetails.Detail}'",
                    newLineAfter:false);

                LogDebug($"Status Code: {Indent}{problemDetails.Status}"
                    + $" - {HttpStatusDetails[problemDetails.Status].Title}");
                LogDebug($"Instance URL: {Indent}'{problemDetails.Instance}'");
                LogDebug($"Type Info: {Indent}'{problemDetails.Type}'");

                LogObject<Dictionary<string, string[]>>(
                    problemDetails.Errors, label: "Errors Dictionary",
                    newLineAfter: false);
                LogObject<Dictionary<string, object>>(
                    problemDetails.Extensions, label: "Extensions Dictionary",
                    newLineAfter: true);
            }
            catch (Exception exception)
            {
                LogException(exception);
            }
        }

        public void LogScalar(string label, string value,
            bool addIndent = false, bool newLineAfter = true)
        {
            var message =
                // Difference in prefix length: "Trace" vs "Information"
                new string(' ', 6) +
                AddIndent(addIndent) + $"{label}{Indent}={Indent}{value}";
            if (newLineAfter)
                message += NewLine;

            _logger.LogTrace("{message}", message);
        }

        public void LogSubheader(string format, params object[] args)
        {
            var message = string.Format(format, args).Trim() + " ";
            var paddedMessage =
                message.PadRight(SubheaderLength, SubheaderChar);

            LogInfo(paddedMessage, newLineAfter: true);
        }

        public void LogTrace(string format, bool addIndent = false,
            bool newLineAfter = false, params object[] args)
        {
            var messagePrefix =
                // Difference in prefix length: "Trace" vs "Information"
                new string(' ', 6) +
                AddIndent(addIndent);

            var message = messagePrefix
                // TODO Handle format + args with separate {}s like Dictionary
                + (args is null || args.Length == 0
                    ? format
                    : string.Format(format, args));

            if (newLineAfter)
                message += NewLine;

            _logger.LogTrace("{message}", message);
        }

        public void LogWarning(string format, bool addIndent = false,
            bool newLineAfter = true, params object[] args)
        {
            var message =
                // Difference in prefix length: "Warning" vs "Information"
                new string(' ', 5) +
                AddIndent(addIndent) + string.Format(format, args);
            if (newLineAfter)
                message += NewLine;

            _logger.LogWarning("{message}", message);
        }

        #endregion

        #region Deprecated Public Methods

        [Obsolete("This method is obsolete. Call LogException instead.",
            error: false)]
        public void LogExceptionRouter(Exception exception)
        {
            LogException(exception);
        }

        [Obsolete("This method is obsolete. Call LogObject instead.",
            error: false)]
        public void LogObjectAsJson<T>(object obj, bool ignore = false,
            bool keepNulls = false, bool newLineAfter = true)
        {
            LogObject<T>(obj, ignore, keepNulls, newLineAfter: newLineAfter);
        }

        [Obsolete("This method is obsolete. Call LogScalar instead.",
            error: false)]
        public void LogValue(string label, string value, bool addIndent = false,
            bool newLineAfter = false)
        {
            LogScalar(label, value, addIndent, newLineAfter);
        }

        #endregion

        #region Private Methods

        // TODO Go back to allowing multiple levels like the old FL library
        private static string AddIndent(bool addExtraIndent)
        {
            // Explicit ident for alignment plus optional one for nesting
            return Indent + (addExtraIndent ? Indent : "");
        }

        private void LogApiException(ApiException apiException)
        {
            var request = apiException.RequestMessage;

            LogCommonException(apiException, "API EXCEPTION");

            LogWarning(
                $"OPERATION:  {request.Method} - {apiException.Uri}" + NewLine);

            if (string.IsNullOrWhiteSpace(apiException.Content))
                return;

            // TODO Document expectation of ProblemDetails or handle alternative
            LogObject<ProblemDetails>(apiException.Content);
        }

        private void LogCommonException(Exception exception, string outerLabel,
            string innerLabel = "INNER EXCEPTION")
        {
            LogWarning($"{outerLabel}:  {exception.Message}{NewLine}");

            var innerExceptionMessage = exception.InnerException?.Message;

            if (string.IsNullOrWhiteSpace(innerExceptionMessage))
                return;

            LogWarning($"{Indent}{innerLabel}:  {innerExceptionMessage}{NewLine}");
        }

        private void LogGeneralException(Exception exception)
        {
            LogCommonException(exception, "EXCEPTION");
        }

        [SuppressMessage("ReSharper", "MergeIntoPattern")]
        private void LogHttpRequestException(HttpRequestException requestException)
        {
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

        private void LogJsonException(JsonException jsonException)
        {
            LogCommonException(jsonException, "JSON EXCEPTION");

            LogWarning($"LINE:  {jsonException.LineNumber}"
                       + $"{Indent}-{Indent}{jsonException.BytePositionInLine}");
            LogWarning($"PATH:  {jsonException.Path}");
        }

        #endregion
    }
}
