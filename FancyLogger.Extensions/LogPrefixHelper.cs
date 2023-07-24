using System.Reflection;
using static System.String;

// Source: https://github.com/xamarinfiles/library-fancy-logger-extensions
//
// Problem: when AssemblyLogger is in a separate library and accessed by another
// library, it triggers an anti-virus flag for assembly passing across libraries
//
// Solution: copy shared project to each repo and add to each test project
namespace XamarinFiles.FancyLogger.Extensions
{
    internal static class LogPrefixHelper
    {
        #region Methods

        // TODO Move to static interface method in IFL after off .NET Std 2.0 (XF)
        // Trims namespace down to use as FancyLogger line prefix:
        //
        // Inputs:
        // RootAssemblyNamespace = "XamarinFiles.FancyLogger."
        // DefaultLogPrefix = "LOG"
        //
        // Outputs:
        // XamarinFiles.FancyLogger.Tests.Smoke.Local => Tests.Smoke.Local
        // XamarinFiles.FancyLogger.Tests.Smoke.NuGet => Tests.Smoke.NuGet
        internal static string GetAssemblyNameTail(Assembly assembly,
            string rootAssemblyNamespace, string defaultLogPrefix)
        {
            var assemblyName = assembly.GetName().Name;
            if (IsNullOrWhiteSpace(assemblyName))
                return "";

            var assemblyNameSpaceTail =
                assemblyName[rootAssemblyNamespace.Length..];

            return IsNullOrEmpty(assemblyNameSpaceTail)
                ? defaultLogPrefix
                : assemblyNameSpaceTail;
        }

        #endregion
    }
}
