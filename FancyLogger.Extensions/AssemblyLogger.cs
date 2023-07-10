using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using static System.Globalization.CultureInfo;
using static XamarinFiles.FancyLogger.Helpers.Characters;

// Source: https://github.com/xamarinfiles/library-fancy-logger-extensions
//
// Problem: when AssemblyLogger is in a shared project and accessed by another
// project, it triggers an anti-virus flag for assembly passing across projects
//
// Solution: copy file to each repo and add to each test project as shared file
// to avoid duplicating code into each test program
namespace XamarinFiles.FancyLogger.Extensions
{
    internal class AssemblyLogger
    {
        #region Fields

        private readonly string _ancestorPath;

        private readonly Assembly _assembly;

        private readonly string _assemblyPath;

        // TODO Change to true or eliminate when add more CultureInfo processing
        private const bool DefaultShowCultureInfo = false;

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
        }

        #endregion

        #region Properties

        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public bool ShowCultureInfo { get; set; }

        #endregion

        #region Methods

        public void LogAssemblies(
            bool showCultureInfo = DefaultShowCultureInfo)
        {
            ShowCultureInfo = showCultureInfo;

            FancyLogger.LogSection(OrganizationPrefix + " Assemblies");

            // Executing Assembly

            LogExecutingAssembly(_assembly);

            // Domain Assemblies

            var domainAssemblies =
                AppDomain.CurrentDomain.GetAssemblies()
                    .ToList()
                    .Where(assembly => assembly.FullName is not null
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

        // TODO Add number of levels up to common ancestor or some generic logic
        private static string GetAncestorPath(string assemblyLocation)
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

        private void LogExecutingAssembly(Assembly? executingAssembly)
        {
            const string executingAssemblyLabel = "Executing Assembly";

            LogAssembly(executingAssemblyLabel, executingAssembly);
        }

        private void LogDomainAssembly(Assembly? domainAssembly)
        {
            const string domainAssemblyLabel = "Domain Assembly";

            LogAssembly(domainAssemblyLabel, domainAssembly);
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

            LogCultureInfo(assemblyName.CultureInfo);

            LogPublicKeyTokenOrLocation(assemblyName, assembly.Location);
        }

        private void LogName(string assemblyLabel,
            AssemblyName assemblyName, bool addIndent = false,
            bool newLineAfter = true)
        {
            FancyLogger.LogInfo($"{assemblyLabel}: {assemblyName.Name}",
                addIndent, newLineAfter);
        }

        private void LogVersion(AssemblyName assemblyName,
            bool addIndent = false, bool newLineAfter = false)
        {
            if (assemblyName.Version is null)
                return;

            FancyLogger.LogScalar("Version" + Indent,
                assemblyName.Version.ToString(), addIndent, newLineAfter);
        }

        private void LogTargetFramework(Assembly assembly,
            bool addIndent = false, bool newLineAfter = false)
        {
            var frameworkAttribute =
                assembly.GetCustomAttribute<TargetFrameworkAttribute>()!;
            var frameworkName = frameworkAttribute.FrameworkDisplayName;

            if (string.IsNullOrEmpty(frameworkName))
                return;

            FancyLogger.LogScalar("Framework", frameworkName,
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

            FancyLogger.LogScalar("Culture" + Indent, cultureName,
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
                FancyLogger.LogScalar("PublicKeyToken", publicKeyToken,
                    addIndent, newLineAfter);
            }
            else if (!string.IsNullOrEmpty(assemblyLocation))
            {
                var relativePath =
                    Path.GetDirectoryName(assemblyLocation[_ancestorPath.Length..]);

                FancyLogger.LogScalar("Location", relativePath,
                    addIndent, newLineAfter);
            }
            else
            {
                // TODO Easy way to get Location of referenced assembly?
                FancyLogger.LogWarning("No Public Key Token or Location",
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

        private void LogReferenceAssembly(AssemblyName? referencedAssemblyName)
        {
            const string referenceAssemblyLabel = "Reference Assembly";

            LogAssemblyName(referenceAssemblyLabel, referencedAssemblyName);
        }

        private void LogAssemblyName(string assemblyNameLabel,
            AssemblyName? assemblyName)
        {
            if (assemblyName == null)
                return;

            LogName(assemblyNameLabel, assemblyName, addIndent: true);

            LogVersion(assemblyName, addIndent: true);

            // TODO Easy way to get Target Framework of referenced assembly?

            LogCultureInfo(assemblyName.CultureInfo, addIndent: true);

            LogPublicKeyTokenOrLocation(assemblyName, assemblyLocation: null,
                addIndent: true);
        }

        #endregion
    }
}
