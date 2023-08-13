using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using XamarinFiles.PdHelpers.Refit.Models;
using static XamarinFiles.PdHelpers.Refit.Enums.ProblemLevel;
using static XamarinFiles.PdHelpers.Refit.Extractors;

// Ignore all JSON serialization/deserialization exceptions here as bad data
#pragma warning disable CA1031

namespace XamarinFiles.FancyLogger.Helpers
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    internal class Serializer
    {
        #region Fields

        private const string AssemblyName = "FancyLogger";

        private const string ComponentName = "Serializer";

        private const int DefaultMaxCharsOnError = 100;

        #endregion

        #region Constructor

        internal Serializer(JsonSerializerOptions readJsonOptions,
            JsonSerializerOptions writeJsonOptions,
            int maxCharsOnError = DefaultMaxCharsOnError)
        {
            SharedReadJsonOptions = readJsonOptions;

            var writeJsonOptionsWithNulls =
                new JsonSerializerOptions(writeJsonOptions)
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.Never
                };
            SharedWriteJsonOptionsWithNulls = writeJsonOptionsWithNulls;

            var writeJsonOptionsWithoutNulls =
                new JsonSerializerOptions(writeJsonOptions)
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };
            SharedWriteJsonOptionsWithoutNulls = writeJsonOptionsWithoutNulls;

            SharedMaxCharsOnError = maxCharsOnError;
        }

        #endregion

        #region Properties

        internal int SharedMaxCharsOnError { get; }

        internal JsonSerializerOptions SharedReadJsonOptions { get; }

        internal JsonSerializerOptions SharedWriteJsonOptionsWithNulls { get; }

        internal JsonSerializerOptions SharedWriteJsonOptionsWithoutNulls { get; }

        #endregion

        #region Methods

        internal (string?, ProblemReport?)
            ToJson<T>(object? obj, bool keepNulls = false,
                int? overrideMaxCharsOnError = null)
        {
            if (obj is null)
                // TODO Case where need to return message about null object?
                return ("", null);

            T typedObject;
            var typeName = typeof(T).Name;
            var maxCharsOnError = overrideMaxCharsOnError ?? SharedMaxCharsOnError;

            // TODO Route when only simple object

            if (obj is string str)
            {
                // TODO Add logic to handle cycles and deep objects > max levels
                // See ReferenceHandler.Preserve on JsonSerializerOptions
                try
                {
                    if (typeof(T) == typeof(string))
                    {
                        // TODO Test if passed in obj is JSON
                        return (str, null);
                    }

                    var deserializedObject =
                        JsonSerializer.Deserialize<T>(str, SharedReadJsonOptions);

                    typedObject = deserializedObject != null
                        ? deserializedObject
                        // Could also be NotSupportedException if JsonConverter
                        : throw new JsonException();
                }
                catch (Exception exception)
                {
                    var developerMessages =
                        FormatDeveloperMessage(
                            $"Unable to deserialize object of type {typeName}",
                            str, maxCharsOnError);

                    var problemReport =
                        ExtractProblemReport(exception,
                            Error,
                            assemblyName: AssemblyName,
                            componentName: ComponentName,
                            operationName: $"Deserialize Object of {typeName}",
                            developerMessages: developerMessages);

                    return (null, problemReport);
                }
            }
            else
            {
                try
                {
                    typedObject = (T)obj;
                }
                catch (Exception exception)
                {
                    var developerMessages =
                        FormatDeveloperMessage(
                            $"Unable to cast object to type {typeName}",
                            obj, maxCharsOnError);

                    var problemReport =
                        ExtractProblemReport(exception,
                            Error,
                            assemblyName: AssemblyName,
                            componentName: ComponentName,
                            operationName: $"Cast Object to {typeName}",
                            developerMessages: developerMessages);

                    return (null, problemReport);
                }
            }

            try
            {
                var writeJsonOptions = CheckKeepNullsToggle(keepNulls);

                var formattedJson =
                    JsonSerializer.Serialize(typedObject, writeJsonOptions);

                return (formattedJson, null);
            }
            catch (Exception exception)
            {
                var developerMessages =
                    FormatDeveloperMessage(
                        $"Unable to serialize object of type {typeName}",
                        typedObject, maxCharsOnError);

                var problemReport =
                    ExtractProblemReport(exception,
                        Error,
                        assemblyName: AssemblyName,
                        componentName: ComponentName,
                        operationName: $"Serialize Object from {typeName}",
                        developerMessages: developerMessages);

                return (null, problemReport);
            }
        }

        private JsonSerializerOptions CheckKeepNullsToggle(bool keepNulls)
        {
            return keepNulls
                ? SharedWriteJsonOptionsWithNulls
                : SharedWriteJsonOptionsWithoutNulls;
        }

        private static string[] FormatDeveloperMessage(string label, object obj,
            int maxChars)
        {
            string message;

            if (obj is string objStr)
            {
                var trimmedObjStr = objStr.Take(maxChars).ToString();

                if (trimmedObjStr?.Length == maxChars)
                    trimmedObjStr += "...";

                message = $"{label}: \"{trimmedObjStr}\"";
            }
            else
            {
                message = $"{label}: \"{obj.GetType().Name}\"";
            }

            return new[] { message };
        }

        #endregion
    }
}
