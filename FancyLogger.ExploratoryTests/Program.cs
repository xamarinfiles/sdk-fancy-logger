using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace FancyLogger.ExploratoryTests
{
    internal static class Program
    {
        private static void Main()
        {
            // TestMultipleSimultaneousLoggers();

            Logger = new FancyLoggerService(OutputTarget.Debug, "LOG: ");

            Logger.WriteHeader("Header");

            Logger.WriteSubheader("Subheader");

            Logger.WriteLine("Hello World!", args: "key");
            Logger.WriteLine("{0}", args: "Hello World!");
            Logger.WriteLine("{0} = {1}", args: new object[] {"key", "value"});

            Logger.WriteNewLine();

            Logger.WriteValue("key", "value");

            Logger.WriteWarning("This is a warning message");

            Logger.WriteInformation("This is an informational message");

            Logger.WriteError("This is an error message");
            Logger.WriteLine("Hello World! {0} = {1}", args: ("key", "value"));

            var dictionary = new Dictionary<string, string>
            {
                { "key", "value" },
                { "hello", "world" },
                { "foo", "bar" },
                { "fizz", "buzz" }
            };
            Logger.WriteDictionary(dictionary);
            Logger.WriteDictionary(dictionary, "Random Word Pairings");

            var list = new List<string>
            {
                "key",
                "value",
                "hello",
                "world",
                "foo",
                "bar",
                "fizz",
                "buzz"
            };
            Logger.WriteList(list);
            Logger.WriteList(list, "Random Word List");

            Logger.WriteException(new Exception());
            Logger.WriteException(new Exception("Exception message"));
            Logger.WriteException(new Exception("Exception message",
                new NullReferenceException("Null reference exception message")));

            try
            {
                var exception = new AggregateException(
                    new List<Exception>
                    {
                        new Exception(),
                        new NullReferenceException(),
                        new IndexOutOfRangeException()
                    });

                throw exception;
            }
            catch (Exception exception)
            {
                Logger.SaveExceptionLocation(exception);
                exception.Data.Add(
                    "Answer to the Ultimate Question of Life, the Universe, and Everything",
                    42);
                Logger.WriteException(exception);
            }

            try
            {
                throw new IndexOutOfRangeException();
            }
            catch (Exception exception)
            {
                Logger.SaveExceptionLocation(exception);
                Logger.WriteException(exception);
            }
        }

        #region Properties

        private static FancyLoggerService ConsoleLogger { get; set; }

        private static FancyLoggerService DebugLogger { get; set; }

        private static FancyLoggerService Logger { get; set; }

        #endregion

        // These are tests of output functionality for display instead of automated tests
        #region Test Methods

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static void TestMultipleSimultaneousLoggers()
        {
            // NOTE To see the console log in Visual Studio:
            // 1. Make sure "Automatically close the console when debugging stops" is off
            //    in Tools/Options/Debugging/General
            // 2. Make sure Project/Application/Output type = Console Application
            ConsoleLogger = new FancyLoggerService(OutputTarget.Console, "CONSOLE: ");
            DebugLogger = new FancyLoggerService(OutputTarget.Debug, "DEBUG: ");

            ConsoleLogger.WriteLine("Hello World!");
            DebugLogger.WriteLine("Hello World!");

            ConsoleLogger = null;
            DebugLogger = null;
        }

        #endregion

    }
}
