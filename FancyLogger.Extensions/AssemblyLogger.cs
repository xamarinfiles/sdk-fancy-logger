using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using static System.Globalization.CultureInfo;
using static XamarinFiles.FancyLogger.Constants.Characters;

// TODO Add blanks to labels based on difference in lengths instead of indent

// Source: https://github.com/xamarinfiles/library-fancy-logger-extensions
//
// Problem: when AssemblyLogger is in a separate library and accessed by another
// library, it triggers an anti-virus flag for assembly passing across libraries
//
// Solution: copy shared project to each repo and add to each test project
namespace XamarinFiles.FancyLogger.Extensions
{
    public class AssemblyLogger
    {
        #region Fields

        private readonly string _ancestorPath;

        private readonly int _ancestorPathLength;

        private readonly Assembly _assembly;

        private readonly string _assemblyPath;

        // TODO Change to true or eliminate when add more CultureInfo processing
        private const bool DefaultShowCultureInfo = false;

        private const string DomainAssemblyLabel = "Domain Assembly";

        private const string ExecutingAssemblyLabel = "Executing Assembly";

        private const string ReferenceAssemblyLabel = "Reference Assembly";

        private const string OrganizationPrefix = "XamarinFiles";

        private const string PackagePrefix = OrganizationPrefix + ".";

        #endregion

        #region Services

        private IFancyLogger FancyLogger { get; }

        #endregion

        #region Constructor

        public AssemblyLogger(IFancyLogger fancyLogger)
        {
            FancyLogger = fancyLogger;

            _assembly = Assembly.GetExecutingAssembly();
            _assemblyPath = _assembly.Location;
            _ancestorPath = GetAncestorPath(_assemblyPath);
            _ancestorPathLength = _ancestorPath.Length;
        }

        #endregion

        #region Properties

        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public bool ShowCultureInfo { get; set; }

        #endregion

        #region Methods

        // TODO Nest assemblies once add indent levels like old FL library
        public void LogAssemblies(
            bool showCultureInfo = DefaultShowCultureInfo)
        {
            ShowCultureInfo = showCultureInfo;

            // Organization Path (assumes same directory base) and Project Path

            LogOrganizationPath();

            // Executing Assembly

            LogExecutingAssembly(_assembly);

            // Domain Assemblies

            var domainAssemblies =
                AppDomain.CurrentDomain.GetAssemblies()
                    .ToList()
                    .Where(assembly => !string.IsNullOrWhiteSpace(assembly.FullName)
                        && assembly.FullName.StartsWith(PackagePrefix)
                        && assembly.Location != _assemblyPath)
                    .OrderBy(assembly => assembly.FullName);

            foreach (var domainAssembly in domainAssemblies)
            {
                LogDomainAssembly(domainAssembly);

                var referenceAssemblyNames =
                    domainAssembly.GetReferencedAssemblies()
                        .ToList()
                        .Where(assemblyName =>
                            assemblyName.FullName.StartsWith(PackagePrefix))
                        .OrderBy(assembly => assembly.FullName);

                foreach (var referencedAssemblyName in referenceAssemblyNames)
                {
                    LogReferenceAssembly(referencedAssemblyName);
                }
            }
        }

        #region Assembly Paths

        // TODO Add number of levels up to common ancestor or some generic logic
        private string GetAncestorPath(string assemblyLocation)
        {
            // Assumes the following directory hierarchy of local source paths:
            // 1.) user or organization
            // 2.) repository
            // 3.) project
            // 4.) bin
            // 5.) configuration (Debug, etc.)
            // 6.) target framework (net7.0, etc.)
            // 7.) library (dll)
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

        private void LogOrganizationPath()
        {
            FancyLogger.LogSection(OrganizationPrefix + " Assemblies");

            FancyLogger.LogScalar("Ancestor Path",
                NewLine + Indent + _ancestorPath, addIndent: true,
                newLineAfter: true);
        }

        private void LogRelativePath(Assembly assembly, bool isExecutingAssembly)
        {
            if (!isExecutingAssembly)
                return;

            var location = assembly.Location;
            var path = location[_ancestorPathLength..];
            var relativePath = Path.GetDirectoryName(path);

            FancyLogger.LogScalar("Relative Path",
                NewLine + Indent + relativePath, addIndent: true,
                newLineAfter: true);
        }

        #endregion

        #region Assembly Types

        private void LogExecutingAssembly(Assembly executingAssembly)
        {
            LogAssembly(executingAssembly, ExecutingAssemblyLabel, true);
        }

        private void LogDomainAssembly(Assembly domainAssembly)
        {
            LogAssembly(domainAssembly, DomainAssemblyLabel);
        }

        private void LogReferenceAssembly(AssemblyName referencedAssemblyName)
        {
            LogAssemblyName(referencedAssemblyName, ReferenceAssemblyLabel);
        }

        private void LogAssembly(Assembly? assembly, string assemblyNameLabel,
            bool isExecutingAssembly = false)
        {
            if (assembly is null)
                return;

            // TODO Account for all cases where AssemblyName is null
            var assemblyName = assembly.GetName();

            LogName(assemblyNameLabel, assemblyName);

            LogRelativePath(assembly, isExecutingAssembly);

            LogVersion(assemblyName);

            LogTargetFramework(assembly);

            LogCommonRuntime(assembly);

            LogBuildConfiguration(assembly);

            LogPublicKeyToken(assemblyName);

            LogCultureInfo(assemblyName.CultureInfo, newLineAfter: true);
        }

        private void LogAssemblyName(AssemblyName? assemblyName,
            string assemblyNameLabel)
        {
            if (assemblyName is null)
                return;

            LogName(assemblyNameLabel, assemblyName);

            LogVersion(assemblyName);

            // TODO Easy way to get from referenced assembly:
            // TargetFramework?
            // ImageRuntimeVersion?

            // TODO
            LogPublicKeyToken(assemblyName);

            LogCultureInfo(assemblyName.CultureInfo, newLineAfter: true);
        }

        #endregion

        #region Access to Common Assembly/AssemblyName Properties

        private void LogName(string assemblyLabel, AssemblyName assemblyName,
            bool newLineAfter = true)
        {
            FancyLogger.LogInfo(
                $"{assemblyLabel}:{Indent}{assemblyName.Name}",
                addIndent: true, newLineAfter: newLineAfter);
        }

        private void LogVersion(AssemblyName assemblyName,
            bool newLineAfter = false)
        {
            if (assemblyName.Version is null)
                return;

            FancyLogger.LogScalar("Version" + Indent + Indent,
                assemblyName.Version.ToString(), addIndent: true,
                newLineAfter: newLineAfter);
        }

        private void LogTargetFramework(Assembly assembly,
            bool newLineAfter = false)
        {
            var frameworkAttribute =
                assembly.GetCustomAttribute<TargetFrameworkAttribute>()!;
            var frameworkName = frameworkAttribute.FrameworkDisplayName;

            if (string.IsNullOrEmpty(frameworkName))
                return;

            FancyLogger.LogScalar("Framework" + Indent,
                frameworkName, addIndent: true,
                newLineAfter: newLineAfter);
        }

        private void LogBuildConfiguration(Assembly assembly,
            bool newLineAfter = false)
        {
            var configurationAttribute =
                assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()!;
            var configuration = configurationAttribute.Configuration;

            if (string.IsNullOrEmpty(configuration))
                return;

            FancyLogger.LogScalar("Configuration",
                configuration, addIndent: true,
                newLineAfter: newLineAfter);
        }

        private void LogCommonRuntime(Assembly assembly,
            bool newLineAfter = false)
        {
            var runtimeVersion = assembly.ImageRuntimeVersion;

            if (string.IsNullOrEmpty(runtimeVersion))
                return;

            FancyLogger.LogScalar("Runtime" + Indent + Indent,
                "CLR " + runtimeVersion, addIndent: true,
                newLineAfter: newLineAfter);
        }

        private void LogCultureInfo(CultureInfo? cultureInfo,
            bool newLineAfter = false)
        {
            if (!ShowCultureInfo || cultureInfo is null)
                return;

            var cultureName = Equals(cultureInfo, InvariantCulture)
                // TODO True even if cultureInfo.IsNeutralCulture says otherwise?
                ? "neutral"
                : cultureInfo.DisplayName;

            FancyLogger.LogScalar("Culture" + Indent + Indent,
                cultureName,
                addIndent: true, newLineAfter: newLineAfter);
        }

        private void LogPublicKeyToken(AssemblyName assemblyName,
            bool newLineAfter = true)
        {
            var publicKeyTokenArray = assemblyName.GetPublicKeyToken();
            var publicKeyTokenStr = GetPublicKeyToken(publicKeyTokenArray);

            if (publicKeyTokenStr == string.Empty)
                return;

            FancyLogger.LogScalar("Public Key" + Indent,
                    publicKeyTokenStr, addIndent: true,
                    newLineAfter: newLineAfter);
        }

        private static string GetPublicKeyToken(byte[]? byteArray)
        {
            var byteString = string.Empty;

            if (byteArray is null || byteArray.Length < 1)
                return byteString;

            for (var i = 0; i < byteArray.GetLength(0); i++)
                byteString += $"{byteArray[i]:x2}";

            return byteString;
        }

        #endregion

        #endregion
    }
}
