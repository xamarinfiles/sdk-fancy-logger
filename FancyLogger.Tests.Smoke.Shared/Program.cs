#define ASSEMBLY_LOGGING

using System;
using System.Diagnostics;
using XamarinFiles.FancyLogger.Extensions;
using XamarinFiles.FancyLogger.Options;
using static System.Net.HttpStatusCode;
using static XamarinFiles.FancyLogger.Enums.ErrorOrWarning;
using static XamarinFiles.PdHelpers.Refit.Bundlers;

namespace XamarinFiles.FancyLogger.Tests.Smoke.Shared
{
    internal static class Program
    {
        #region Fields

        // TODO Set to same default as FancyLoggerOptions.AllLines.PrefixString
        private const string DefaultLogPrefix = "LOG";

        private const string LoginFailedTitle = "Invalid Credentials";

        private static readonly string[] LoginFailedUserMessages =
        {
            "Please check your Username and Password and try again"
        };

        private const string RootAssemblyNamespace = "XamarinFiles.FancyLogger.";

        #endregion

        #region Services

        private static IFancyLogger? FancyLogger { get; }

#if ASSEMBLY_LOGGING
        private static AssemblyLogger? AssemblyLogger { get; }
#endif

        #endregion

        #region Constructor

        static Program()
        {
            try
            {
                var assembly = typeof(Program).Assembly;

                // ReSharper disable once UseObjectOrCollectionInitializer
                var options = new FancyLoggerOptions();
                options.AllLines.PrefixString =
                    LogPrefixHelper.GetAssemblyNameTail(assembly,
                        RootAssemblyNamespace, DefaultLogPrefix);

                FancyLogger = new FancyLogger(loggerOptions: options);
#if ASSEMBLY_LOGGING
                AssemblyLogger = new AssemblyLogger(FancyLogger);
#endif
            }
            catch (Exception exception)
            {
                if (FancyLogger is not null)
                {
                    FancyLogger.LogException(exception);
                }
                else
                {
                    Debug.WriteLine("ERROR: Problem setting up logging services");
                    Debug.WriteLine(exception);
                }
            }
        }

        #endregion

        #region Test Runner

        internal static void Main()
        {
            try
            {
                if (FancyLogger is null)
                {
                    return;
                }

#if ASSEMBLY_LOGGING
                AssemblyLogger?.LogAssemblies(showCultureInfo: true);
#endif
                FancyLogger.LogSection("Run FancyLogger Tests");

                // TODO Add updated test set from old Fancy Logger

                TestPrefixOverrides();

                TestStructuralLoggingMethods();

                TestSpecializedLoggingMethods();
            }
            catch (Exception exception)
            {
                FancyLogger?.LogException(exception);
            }
        }

        #endregion

        #region Tests

        private static void TestPrefixOverrides()
        {
            FancyLogger!.LogSection("Prefix Override Tests");

            var loggerOptions = new FancyLoggerOptions
            {
                AllLines =
                {
                    PrefixString = "Options Prefix",
                    PadLength = 17,
                }
            };

            var defaultPrefixFancyLogger =
                new FancyLogger();
            defaultPrefixFancyLogger.LogInfo(
                "Test All Lines Prefix From Defaults");

            var optionsPrefixFancyLogger =
                new FancyLogger(loggerOptions: loggerOptions);
            optionsPrefixFancyLogger.LogInfo(
                "Test All Lines Prefix From Options");

            var argumentPrefixFancyLogger =
                new FancyLogger(allLinesPrefix: "Argument Prefix",
                    allLinesPadLength: 20, loggerOptions: loggerOptions);
            argumentPrefixFancyLogger.LogInfo(
                "Test All Lines Prefix From Arguments");
        }

        private static void TestSpecializedLoggingMethods()
        {
            FancyLogger!.LogSection("Specialized Logging Tests");

            TestProblemDetailsMethod();
        }

        private static void TestProblemDetailsMethod()
        {
            FancyLogger!.LogSubsection("ProblemDetails Logging Tests");

            // 400 - BadRequest - Error

            var badRequestProblem =
                BundleRefitProblemDetails(BadRequest,
                    title: LoginFailedTitle,
                    detail: "Invalid fields: Username, Password",
                    developerMessages: new[]
                    {
                        "The Username field is required.",
                        "The Password field is required."
                    },
                    userMessages: LoginFailedUserMessages);

            FancyLogger.LogProblemDetails(badRequestProblem, Error);

            // 401 - Unauthorized - Warning

            var unauthorizedProblem =
                BundleRefitProblemDetails(Unauthorized,
                    title: LoginFailedTitle,
                    detail : "Username and/or Password do not match",
                    userMessages: LoginFailedUserMessages);

            FancyLogger.LogProblemDetails(unauthorizedProblem, Warning);

            // TODO Add other ProblemDetails tests from other repo
        }

        private static void TestStructuralLoggingMethods()
        {
            FancyLogger!.LogSection("Structural Logging Tests");

            FancyLogger.LogLongDividerLine();

            FancyLogger.LogSubsection("Subsection One");

            FancyLogger.LogHeader("Header", addStart: true);

            FancyLogger.LogFooter("Footer", addEnd: true);

            FancyLogger.LogSubsection("Subsection Two");

            FancyLogger.LogShortDividerLine();
        }

        #endregion
    }
}
