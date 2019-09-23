using FancyLogger;
using System.Diagnostics;

namespace FancyLogger.Console
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            FancyLogger = new FancyLoggerService();

            System.Console.WriteLine("Hello World!");
            Debug.WriteLine("Hello World!");
        }

        #region Properties

        private static FancyLoggerService FancyLogger { get; set; }

        #endregion
    }
}
