#define ASSEMBLY_LOGGING

using System;
using System.Diagnostics;
using System.Reflection;
using XamarinFiles.FancyLogger.Extensions;
using XamarinFiles.FancyLogger.Tests.Smoke.Local;
using static System.Net.HttpStatusCode;
using static System.String;
using static System.StringSplitOptions;
using static XamarinFiles.PdHelpers.Refit.Bundlers;

namespace XamarinFiles.FancyLogger.Tests.Smoke.Shared
{
    internal static class Program
    {
        #region Fields

        private const string AssemblyNamespacePrefix =
            "XamarinFiles.FancyLogger.";

        private const string DefaultLogPrefix = "LOG";

        private const string LoginFailedTitle = "Invalid Credentials";

        private static readonly string[] LoginFailedUserMessages =
        {
            "Please check your Username and Password and try again"
        };

        #endregion

        #region Services

        private static IFancyLogger? FancyLogger { get; }

#if ASSEMBLY_LOGGING
        private static IAssemblyLogger? AssemblyLogger { get; }
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
                options.AllLines.PrefixString = GetLogPrefix(assembly);

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

                // TODO Add updated test set from old Fancy Logger

                TestProblemDetailsLogger();

                FancyLogger.LogLongDividerLine();

                FancyLogger.LogSection("Structural Logging Tests");

                FancyLogger.LogSubsection("Subsection One");

                FancyLogger.LogHeader("Header", addStart: true);

                FancyLogger.LogFooter("Footer", addEnd: true);

                FancyLogger.LogSubsection("Subsection Two");

                FancyLogger.LogShortDividerLine();
            }
            catch (Exception exception)
            {
                FancyLogger?.LogException(exception);
            }
        }

        #endregion

        #region Tests

        private static void TestProblemDetailsLogger()
        {
            // 400 - BadRequest

            var badRequestProblem =
                BundleRefitProblemDetails(BadRequest,
                    title: LoginFailedTitle,
                    detail : "Invalid fields: Username, Password",
                    userMessages: LoginFailedUserMessages);

            FancyLogger!.LogProblemDetails(badRequestProblem);

            // TODO Add other ProblemDetails tests from other repo
        }

        #endregion

        #region Helpers

        private static string GetLogPrefix(Assembly assembly)
        {
            var assemblyName = assembly.GetName().Name;
            if (IsNullOrWhiteSpace(assemblyName))
                return "";

            var shortName =
                assemblyName.Split(AssemblyNamespacePrefix,
                    RemoveEmptyEntries);

            if (shortName.Length < 1)
                // TODO Move to Fields
                return DefaultLogPrefix;

            var logPrefix = shortName[0];

            return logPrefix;
        }

        #endregion
    }
}
