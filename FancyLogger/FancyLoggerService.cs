using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace FancyLogger
{
    public class FancyLoggerService
    {
        // TODO Turn these into user-settable properties
        #region Fields

        private const string ErrorPrefix = "ERROR: ";

        private const string ExceptionPrefix = "EXCEPTION: ";

        private const string HeaderPrefix = "@@@ ";

        private const string InformationPrefix = "INFORMATION: ";

        private const string SubheaderPrefix = "*** ";

        private const string WarningPrefix = "WARNING: ";

        private const int WrapColumn = 80;

        #endregion

        #region Constructor

        public FancyLoggerService(OutputTarget target = OutputTarget.Debug,
            string globalPrefix = "")
        {
            OutputTarget = target;
            GlobalPrefix = globalPrefix;
        }

        #endregion

        #region Properties

        // NOTE Tell user that no default spacing is added before or after DefaultPrefix
        public string GlobalPrefix { get; private set; }

        public OutputTarget OutputTarget { get; private set; }

        #endregion

        #region Methods

        #region WriteDictionary

        public void WriteDictionary<TKey, TValue>(IDictionary<TKey, TValue> dictionary,
            string typeLabel = null, string linePrefix = "", bool newlineBefore = true,
            bool newlineAfter = false, uint indentLevel = 1)
        {
            try
            {
                var dictionaryType = typeLabel ??
                    $"Dictionary<{typeof(TKey).Name},{typeof(TValue).Name}>";

                WriteSubheader($"|{dictionaryType}| = {dictionary.Count}",
                    linePrefix, newlineBefore, false, indentLevel);

                var keys = dictionary.Keys.ToArray();
                var values = dictionary.Values.ToArray();

                for (var index = 0; index < dictionary.Count; index++)
                {
                    var key = keys[index];
                    var value = values[index];

                    // TODO Make sure key and value are simple object types or printable
                    // TODO Left-align index value by digits in dictionary count
                    WriteLine($"{index + 1,-3} : {key} => {value}", linePrefix,
                        false, false, indentLevel + 1);
                }

                if (newlineAfter)
                    WriteNewLine();
            }
            catch (Exception exception)
            {
                SaveExceptionLocation(exception);
                WriteException(exception);
            }
        }

        #endregion

        #region WriteError

        // NOTE Doesn't allow passed args to keep same pattern as other methods
        public void WriteError(string error, string linePrefix = ErrorPrefix,
            bool newlineBefore = true, bool newlineAfter = false, uint indentLevel = 1)
        {
            try
            {
                WriteSubheader(linePrefix + "{0}", "", newlineBefore,
                    newlineAfter, indentLevel, args: error);
            }
            catch (Exception exception)
            {
                SaveExceptionLocation(exception);
                WriteException(exception);
            }
        }

        #endregion

        #region WriteException

        public void WriteException(Exception exception, bool showStackTrace = false,
            string linePrefix = ExceptionPrefix, bool newlineBefore = true,
            bool newlineAfter = false, uint indentLevel = 1)
        {
            try
            {
                var exceptionName = exception.GetType().Name;
                var exceptionMessage =
                    exception.InnerException?.Message ?? exception.Message;
                var exceptionArgs = new object[] { exceptionName, exceptionMessage };

                WriteSubheader("{0} - {1}", linePrefix, newlineBefore,
                    false, indentLevel, exceptionArgs);

                WriteExceptionDetails(exception, showStackTrace);

                if (!(exception is AggregateException aggregateException))
                    return;

                foreach (var innerException in aggregateException.InnerExceptions)
                {
                    WriteNewLine();

                    WriteValue("Inner Exception", innerException.Message ?? "");
                    WriteExceptionDetails(innerException, showStackTrace);
                }
            }
            catch (Exception followupException)
            {
                Debug.WriteLine(followupException);
            }
        }

        private void WriteExceptionDetails(Exception exception, bool showStackTrace)
        {
            if (!string.IsNullOrEmpty(exception.TargetSite?.DeclaringType?.FullName))
                WriteValue("DeclaringType", exception.TargetSite.DeclaringType.FullName);
            else if (!string.IsNullOrEmpty(exception.Source))
                WriteValue("Source", exception.Source);
            else
            {
                if (exception.Data.Contains("Member Name"))
                    WriteValue("Member Name", exception.Data["Member Name"]);

                if (exception.Data.Contains("Source File Path"))
                    WriteValue("Source File Path", exception.Data["Source File Path"]);
            }

            if (exception.TargetSite != null)
                WriteValue("TargetSite", exception.TargetSite);

            foreach (DictionaryEntry entry in exception.Data)
            {
                var key = entry.Key.ToString();

                if (key != "Member Name" && key != "Source File Path")
                    WriteValue(key, entry.Value);
            }

            // TODO Pass in exception handler for Refit + 3rd party + custom exceptions
            //switch (ex)
            //{
                //case ApiException apiException:
                //    var requestMessage = apiException.RequestMessage;

                //    WriteLine($"{requestMessage.Method} {requestMessage.RequestUri}");
                //    // TODO Print parameters if POST

                //    break;
            //}

            if (showStackTrace)
                WriteValue("StackTrace", exception.StackTrace);
        }

        #endregion

        #region WriteHeader

        public void WriteHeader(string header, string linePrefix = HeaderPrefix,
            bool newlineBefore = true, bool newlineAfter = false, uint indentLevel = 0,
            params object[] args)
        {
            try
            {
                WriteLine(header, linePrefix, newlineBefore, newlineAfter,
                    indentLevel, args);
            }
            catch (Exception exception)
            {
                SaveExceptionLocation(exception);
                WriteException(exception);
            }
        }

        #endregion

        #region WriteInformation

        // NOTE Doesn't allow passed args to keep same pattern as other methods
        public void WriteInformation(string information,
            string linePrefix = InformationPrefix, bool newlineBefore = true,
            bool newlineAfter = false, uint indentLevel = 1)
        {
            try
            {
                WriteSubheader(linePrefix + "{0}", "", newlineBefore,
                    newlineAfter, indentLevel, args: information);
            }
            catch (Exception exception)
            {
                SaveExceptionLocation(exception);
                WriteException(exception);
            }
        }

        #endregion

        #region WriteLine

        public void WriteLine(string format, string linePrefix = "",
            bool newlineBefore = false, bool newlineAfter = false,  uint indentLevel = 2,
            params object[] args)
        {
            try
            {
                if (string.IsNullOrEmpty(format))
                    return;

                string message;
                var indentPrefix = GlobalPrefix + new string('\t', (int) indentLevel);
                var indentedLinePrefix = indentPrefix + linePrefix;


                if (newlineBefore)
                    WriteNewLine();

                if (args == null || args.Length < 1)
                {
                    message = indentedLinePrefix + format;
                }
                else if (args.Length > 2 || IsSimpleObjectOrArray(args[0]))
                {
                    var formatted = string.Format(format, args);

                    if (indentedLinePrefix.Length + formatted.Length > WrapColumn)
                    {
                        message = indentedLinePrefix + "\n";
                        message += indentPrefix + new string('\t', 1) + formatted;
                    }
                    else
                    {
                        message = indentedLinePrefix + formatted;
                    }

                    //if (linePrefix.Length > 0)
                    //{
                    //    //message = prefix;

                    //    //if (prefix.Length + formatted.Length > WrapColumn)
                    //    //    message += "\n" + GlobalPrefix +
                    //    //               new string('\t', (int) indentLevel + 1);

                    //    message += formatted;
                    //}
                    //else
                    //{
                    //    message = formatted;
                    //}
                }
                else
                {
                    WriteError(
                        "'Args' is not a simple object or array like in String.Format");

                    return;
                }

                switch (OutputTarget)
                {
                    case OutputTarget.Debug:
                        Debug.WriteLine(message);

                        break;
                    case OutputTarget.Console:
                        Console.WriteLine(message);

                        break;
                }

                if (newlineAfter)
                    WriteNewLine();
            }
            catch (Exception exception)
            {
                SaveExceptionLocation(exception);
                WriteException(exception);
            }
        }

        private bool IsSimpleObjectOrArray(object value)
        {
            var type = value.GetType();

            return type.IsPrimitive
                   || type.IsEnum
                   || type == typeof(string)
                   || type == typeof(decimal)
                   || type.IsArray;
        }

        #endregion

        #region WriteList

        public void WriteList<T>(IList<T> list, string typeLabel = null,
            string linePrefix = "", bool newlineBefore = true, bool newlineAfter = false,
            uint indentLevel = 1)
        {
            try
            {
                var listType = typeLabel ?? $"List<{typeof(T).Name}>";

                WriteSubheader($"|{listType}| = {list.Count}", linePrefix, newlineBefore,
                    false, indentLevel);

                for (var index = 0; index < list.Count; index++)
                {
                    var element = list[index];

                    // TODO Make sure element is a simple object types or printable
                    // TODO Left-align index value by digits in list count
                    WriteLine($"{index,-3} : {element}");
                }

                if (newlineAfter)
                    WriteNewLine();
            }
            catch (Exception exception)
            {
                SaveExceptionLocation(exception);
                WriteException(exception);
            }
        }

        #endregion

        #region WriteNewLine

        public void WriteNewLine()
        {
            switch (OutputTarget)
            {
                case OutputTarget.Debug:
                    Debug.WriteLine(GlobalPrefix + "");
                    break;
                case OutputTarget.Console:
                    Console.WriteLine(GlobalPrefix + "");
                    break;
            }
        }

        #endregion

        #region WriteSubheader

        public void WriteSubheader(string subheader, string linePrefix = SubheaderPrefix,
            bool newlineBefore = true, bool newlineAfter = false, uint indentLevel = 1,
            params object[] args)
        {
            try
            {
                WriteLine(subheader, linePrefix, newlineBefore, newlineAfter, indentLevel,
                    args);
            }
            catch (Exception exception)
            {
                SaveExceptionLocation(exception);
                WriteException(exception);
            }
        }

        #endregion

        #region WriteValue

        public void WriteValue(string name, object value, string linePrefix = "",
            bool newlineBefore = false, bool newlineAfter = false, uint indentLevel = 2)
        {
            try
            {
                const string format = "{0} = {1}";

                string message;
                var indentPrefix = GlobalPrefix + new string('\t', (int) indentLevel);
                var indentedLinePrefix = indentPrefix + linePrefix;
                var formatted = string.Format(format, name, value);

                if (indentedLinePrefix.Length + formatted.Length > WrapColumn)
                {
                    message = $"{name} = \n";
                    message += $"{indentedLinePrefix}{new string('\t', 1)}{value}";
                }
                else
                {
                    message = formatted;
                }

                //WriteLine(format, linePrefix, newlineBefore, newlineAfter, indentLevel,
                //    name, value);
                WriteLine(message, linePrefix, newlineBefore, newlineAfter,
                    indentLevel, null);
            }
            catch (Exception exception)
            {
                SaveExceptionLocation(exception);
                WriteException(exception);
            }
        }

        #endregion

        #region WriteWarning


        // NOTE Doesn't allow passed args to keep same pattern as other methods
        public void WriteWarning(string warning, string linePrefix = WarningPrefix,
            bool newlineBefore = true, bool newlineAfter = false, uint indentLevel = 1)
        {
            try
            {
                WriteSubheader(linePrefix + "{0}", "", newlineBefore,
                    newlineAfter, indentLevel, args: warning);
            }
            catch (Exception exception)
            {
                SaveExceptionLocation(exception);
                WriteException(exception);
            }
        }

        #endregion

        #region Exceptions

        public void SaveExceptionLocation(Exception exception,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            SaveExceptionValue("Member Name", memberName);
            SaveExceptionValue("Source File Path", sourceFilePath);
            SaveExceptionValue("Source Line Number", sourceLineNumber);

            void SaveExceptionValue(string key, object value)
            {
                // TODO Add scheme to auto-increment key and insert
                if (!exception.Data.Contains(key))
                {
                    exception.Data.Add(key, value);
                }
            }
        }

        #endregion

        #endregion
    }
}
