#define ASSEMBLY_LOGGING

using System.Diagnostics;
using System;
using XamarinFiles.FancyLogger.Extensions;
using static System.Net.HttpStatusCode;
using static XamarinFiles.PdHelpers.Refit.Bundlers;

namespace XamarinFiles.FancyLogger.Tests.Smoke.Shared
{
    internal static class Program
    {
        #region Fields

        private const string LoginFailedTitle = "Invalid Credentials";

        private static readonly string[] LoginFailedUserMessages =
        {
            "Please check your Username and Password and try again"
        };

        #endregion

        #region Services

        private static FancyLoggerService? LoggerService { get; }

#if ASSEMBLY_LOGGING
        private static AssemblyLoggerService? AssemblyLoggerService { get; }
#endif

        #endregion

        #region Constructor

        static Program()
        {
            try
            {

                LoggerService = new FancyLoggerService();
#if ASSEMBLY_LOGGING
                AssemblyLoggerService = new AssemblyLoggerService(LoggerService);
#endif
            }
            catch (Exception exception)
            {
                if (LoggerService is not null)
                {
                    LoggerService.LogException(exception);
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
                if (LoggerService is null)
                {
                    return;
                }

#if ASSEMBLY_LOGGING
                AssemblyLoggerService?.LogAssemblies(showCultureInfo: true);
#endif

                LoggerService?.LogHeader("FancyLogger Tests");

                // TODO Add updated test set from old Fancy Logger

                LoggerService?.LogHeader("Header", addStart: true);

                LoggerService?.LogSubheader("Subheader");

                TestProblemDetailsLogger();

                LoggerService?.LogFooter("Footer", addEnd: true);
            }
            catch (Exception exception)
            {
                LoggerService?.LogException(exception);
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

            LoggerService!.LogProblemDetails(badRequestProblem);

            // TODO Add other ProblemDetails tests from other repo
        }

        #endregion
    }
}
