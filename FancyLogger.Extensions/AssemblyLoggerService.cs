using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using static System.Globalization.CultureInfo;
using static XamarinFiles.FancyLogger.Characters;

namespace XamarinFiles.FancyLogger.Extensions
{
    // Copy to each tester until get around anti-virus flag for assembly passing
    internal class AssemblyLoggerService
    {
        #region Fields

        private readonly string AncestorPath;

        private readonly string AssemblyPath;

        private const string OrganizationPrefix = "XamarinFiles";

        private const string PackagePrefix = OrganizationPrefix + ".";

        // TODO Change to true or eliminate when add more CultureInfo processing
        private const bool DefaultShowCultureInfo = true;

        #endregion

        #region Services

        private FancyLoggerService LoggerService { get; }

        #endregion

        #region Constructor

        public AssemblyLoggerService(FancyLoggerService loggerService)
        {
            LoggerService = loggerService;
            AssemblyPath = Assembly.GetExecutingAssembly().Location;
            AncestorPath = GetAncestorPath(AssemblyPath);
        }

        #endregion

        #region Properties

        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public bool ShowCultureInfo { get; set; }

        #endregion

        #region Methods

        internal void LogAssemblies(
            bool showCultureInfo = DefaultShowCultureInfo)
        {
            ShowCultureInfo = showCultureInfo;

            const string executingAssemblyLabel = "Executing Assembly";
            const string domainAssemblyLabel = "Domain Assembly";
            const string referenceAssemblyLabel = "Reference Assembly";

            LoggerService.LogHeader(OrganizationPrefix + " Assemblies");

            // Executing Assembly

            var executingAssembly = Assembly.GetExecutingAssembly();

            LogAssembly(executingAssemblyLabel, executingAssembly);

            // Domain Assemblies

            var domainAssemblies =
                AppDomain.CurrentDomain.GetAssemblies()
                    .ToList()
                    .Where(assembly => assembly.FullName is not null
                        && assembly.FullName.StartsWith(PackagePrefix)
                        && assembly.Location != AssemblyPath)
                    .OrderBy(assembly => assembly.FullName);

            foreach (var domainAssembly in domainAssemblies)
            {

                LogAssembly(domainAssemblyLabel, domainAssembly);

                var referenceAssemblyNames =
                    domainAssembly.GetReferencedAssemblies()
                        .ToList()
                        .Where(assemblyName => assemblyName?.FullName is not null
                            && assemblyName.FullName.StartsWith(PackagePrefix))
                        .OrderBy(assembly => assembly.FullName);                    ;

                foreach (var referencedAssemblyName in referenceAssemblyNames)
                {
                    LogAssemblyName(referenceAssemblyLabel,
                        referencedAssemblyName);
                }
            }
        }

        // TODO Add number of levels up to common ancestor or some generic logic
        private static string GetAncestorPath(string assemblyLocation)
        {
            // Assumes the following directory hierarchy of local source paths:
            // user or organization
            // repository
            // project
            // bin
            // configuration (Debug, etc.)
            // target framework (net7.0, etc.)
            // library (dll)
            var ancestorPath =
                Path.GetFullPath(
                    Path.Combine(
                        Path.Combine(
                            // Start at the the library directory
                            assemblyLocation,
                            // Go up to the project directory
                            "..", "..", "..", ".."),
                        // Go up to the user or organization directory
                        "..", ".."));

            return ancestorPath;
        }

        private void LogAssembly(string assemblyNameLabel, Assembly? assembly)
        {
            if (assembly == null)
                return;

            // TODO Account for all cases where AssemblyName is null
            var assemblyName = assembly.GetName();

            LogName(assemblyNameLabel, assemblyName);

            LogVersion(assemblyName);

            LogTargetFramework(assembly);

            LogBuildConfiguration(assembly);

            LogCultureInfo(assemblyName.CultureInfo);

            LogPublicKeyTokenOrLocation(assemblyName, assembly.Location);
        }

        private void LogName(string assemblyLabel,
            AssemblyName assemblyName, bool addIndent = false,
            bool newLineAfter = false)
        {
            LoggerService.LogInfo($"{assemblyLabel}: {assemblyName.Name}",
                addIndent, newLineAfter);
        }

        private void LogVersion(AssemblyName assemblyName,
            bool addIndent = false, bool newLineAfter = false)
        {
            if (assemblyName.Version is null)
                return;

            LoggerService.LogValue("Version" + Indent,
                assemblyName.Version.ToString(), addIndent, newLineAfter);
        }

        private void LogTargetFramework(Assembly assembly,
            bool addIndent = false, bool newLineAfter = false)
        {
            var frameworkAttribute =
                assembly.GetCustomAttribute<TargetFrameworkAttribute>()!;
            var frameworkName = frameworkAttribute?.FrameworkDisplayName;

            if (string.IsNullOrEmpty(frameworkName))
                return;

            LoggerService.LogValue("Framework", frameworkName,
                addIndent, newLineAfter);
        }

        private void LogBuildConfiguration(Assembly assembly,
            bool addIndent = false, bool newLineAfter = false)
        {
            var debuggableAttribute =
                assembly.GetCustomAttribute<DebuggableAttribute>();
            var isDebugStr = debuggableAttribute != null ? "YES" : "NO";

            LoggerService.LogValue("Debug Build",isDebugStr,
                addIndent, newLineAfter);
        }

        private void LogCultureInfo(CultureInfo? cultureInfo,
            bool addIndent = false, bool newLineAfter = false)
        {
            if (!ShowCultureInfo || cultureInfo is null)
                return;

            var cultureName = Equals(cultureInfo, InvariantCulture)
                // TODO True even if cultureInfo.IsNeutralCulture says otherwise?
                ? "neutral"
                : cultureInfo.DisplayName;

            LoggerService.LogValue("Culture" + Indent, cultureName,
                addIndent, newLineAfter);
        }

        private void LogPublicKeyTokenOrLocation(
            AssemblyName assemblyName, string? assemblyLocation = null,
            bool addIndent = false, bool newLineAfter = true)
        {
            var publicKeyToken =
                GetPublicKeyToken(assemblyName.GetPublicKeyToken());

            if (publicKeyToken != string.Empty)
            {
                LoggerService.LogValue("PublicKeyToken",publicKeyToken,
                    addIndent, newLineAfter);
            }
            else if (!string.IsNullOrEmpty(assemblyLocation))
            {
                var relativePath =
                    Path.GetDirectoryName(assemblyLocation[AncestorPath.Length..]);

                LoggerService.LogValue("Location",relativePath,
                    addIndent, newLineAfter);
            }
            else
            {
                // TODO Easy way to get Location of referenced assembly?
                LoggerService.LogWarning("No Public Key Token or Location",
                    addIndent, newLineAfter);
            }
        }

        private static string GetPublicKeyToken(byte[]? byteArray)
        {
            var byteString = string.Empty;

            if (byteArray is not { Length: > 0 })
                return byteString;

            for (var i = 0; i < byteArray.GetLength(0); i++)
                byteString += $"{byteArray[i]:x2}";

            return byteString;
        }

        private void LogAssemblyName(string assemblyNameLabel,
            AssemblyName? assemblyName)
        {
            if (assemblyName == null)
                return;

            LogName(assemblyNameLabel, assemblyName, addIndent: true);

            LogVersion(assemblyName, addIndent: true);

            // TODO Easy way to get Build Configuration of referenced assembly?

            // TODO Easy way to get Target Framework of referenced assembly?

            LogCultureInfo(assemblyName.CultureInfo, addIndent: true);

            LogPublicKeyTokenOrLocation(assemblyName, assemblyLocation: null,
                addIndent: true);
        }

#endregion

    }
}
