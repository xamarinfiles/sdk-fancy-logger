using Refit;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using static XamarinFiles.PdHelpers.Refit.Extractors;

namespace XamarinFiles.FancyLogger.Helpers
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    internal class Serializer
    {
        #region Fields

        #endregion

        #region Constructor

        internal Serializer(JsonSerializerOptions readJsonOptions,
            JsonSerializerOptions writeJsonOptions)
        {
            SharedReadJsonOptions = readJsonOptions;

            writeJsonOptions.DefaultIgnoreCondition =
                JsonIgnoreCondition.Never;
            SharedWriteJsonOptionsWithNulls = writeJsonOptions;

            writeJsonOptions.DefaultIgnoreCondition =
                JsonIgnoreCondition.WhenWritingNull;
            SharedWriteJsonOptionsWithoutNulls = writeJsonOptions;
        }

        #endregion

        #region Properties

        internal JsonSerializerOptions SharedReadJsonOptions { get; }

        internal JsonSerializerOptions SharedWriteJsonOptionsWithNulls { get; }

        internal JsonSerializerOptions SharedWriteJsonOptionsWithoutNulls { get; }

        #endregion

        #region Methods

        internal (string, ProblemDetails)
            ToJson<T>(object obj, bool keepNulls = false)
        {
            if (obj is null)
                // TODO Case where need to return message about null object?
                return ("", null);

            T typedObject;

            // TODO Route when only simple object

            if (obj is string str)
            {
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
                    // TODO Only show beginning of long string in debug message
                    var debugMessage =
                        $"Unable to deserialize object of type {nameof(T)}: \"{str}\"";
                    var problemDetails =
                        ExtractRefitProblemDetails(exception,
                            developerMessages: new[] { debugMessage });

                    return (null, problemDetails);
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
                    // TODO Only show beginning of large object in debug message
                    var debugMessage =
                        $"Unable to cast object to type {nameof(T)}: \"{obj}\"";

                    var problemDetails =
                        ExtractRefitProblemDetails(exception,
                            developerMessages: new[] { debugMessage });

                    return (null, problemDetails);
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
                // TODO Only show beginning of large object in debug message
                var debugMessage =
                    $"Unable to serialize object of type {nameof(T)}: \"{typedObject}\"";

                var problemDetails =
                    ExtractRefitProblemDetails(exception,
                        developerMessages: new[] { debugMessage });

                return (null, problemDetails);
            }
        }

        private JsonSerializerOptions CheckKeepNullsToggle(bool keepNulls)
        {
            return keepNulls
                ? SharedWriteJsonOptionsWithNulls
                : SharedWriteJsonOptionsWithoutNulls;
        }

        #endregion
    }
}
