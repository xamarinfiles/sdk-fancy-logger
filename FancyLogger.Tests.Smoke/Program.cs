using System.Diagnostics;
using System;
using XamarinFiles.FancyLogger.Extensions;
using static System.Net.HttpStatusCode;
using static XamarinFiles.PdHelpers.Refit.Bundlers;

namespace XamarinFiles.FancyLogger.Tests.Smoke
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

        private static AssemblyLoggerService? AssemblyLoggerService { get; }

        #endregion

        #region Constructor

        static Program()
        {
            try
            {

                LoggerService = new FancyLoggerService();
                AssemblyLoggerService = new AssemblyLoggerService(LoggerService);
            }
            catch (Exception exception)
            {
                if (LoggerService is not null)
                {
                    LoggerService.LogExceptionRouter(exception);
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
                /*  WARNING - Requires shared project copied from other repo  */

                AssemblyLoggerService?.LogAssemblies(showCultureInfo: false);

                /* /\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\ */

                if (LoggerService is null)
                {
                    return;
                }

                LoggerService?.LogHeader("FancyLogger Tests");

                // TODO Add updated test set from old Fancy Logger

                TestProblemDetailsLogger();
            }
            catch (Exception exception)
            {
                LoggerService?.LogExceptionRouter(exception);
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
