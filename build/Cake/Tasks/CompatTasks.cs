using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using static CakeScript.Startup;
using static CakeScript.CakeAPI;
using Cake.Common.IO;
using Cake.Common.Tools.DotNetCore.MSBuild;
using Cake.Common.Tools.NuGet.Pack;

namespace CakeScript
{
    partial class Program
    {
        [DependsOn(nameof(PrepareNuget))]
        public void PackCompat()
        {
            string solutionFile = Path.Combine(solutionDirectory, solutionFilename);
            string usedPackagesVersionPath = Path.Combine(solutionDirectory, "UsedPackages.version");

            string nugetDir = Path.Combine(solutionDirectory, "bin", IsRelease ? "nuget" : config);

            // Clean nuget directory for package
            if (DirectoryExists(nugetDir))
            {
                DeleteDirectory(nugetDir, new DeleteDirectorySettings
                {
                    Force = true,
                    Recursive = true
                });
            }

            TargetBuildCore("Clean");

            TargetBuildCore("Restore");

            TargetBuildCore("Build");

            TargetBuildCore("PrepareCompatPackage");

            // Get used packages version
            string SystemDrawingCommonVersion = XmlPeek(usedPackagesVersionPath, "//SystemDrawingCommonVersion/text()");
            Information($"System.Drawing.Common version: {SystemDrawingCommonVersion}");
            string CodeAnalysisCSharpVersion = XmlPeek(usedPackagesVersionPath, "//CodeAnalysisCSharpVersion/text()");
            Information($"Microsoft.CodeAnalysis.CSharp version: {CodeAnalysisCSharpVersion}");
            string CodeAnalysisVisualBasicVersion = XmlPeek(usedPackagesVersionPath, "//CodeAnalysisVisualBasicVersion/text()");
            Information($"Microsoft.CodeAnalysis.VisualBasic version: {CodeAnalysisVisualBasicVersion}");


            var dependencies = new List<NuSpecDependency>();
            AddNuSpecDep(null, null, ".NETFramework4.0");
            // System.Drawing.Common reference doesn't included in net5.0-windows target
            AddNuSpecDep("System.Drawing.Common", SystemDrawingCommonVersion, tfmStandard20);
            AddNuSpecDep("System.Drawing.Common", SystemDrawingCommonVersion, tfmStandard21);
            AddNuSpecDep("System.Drawing.Common", SystemDrawingCommonVersion, tfmCore30);
            AddNuSpecDepCore("Microsoft.CodeAnalysis.CSharp", CodeAnalysisCSharpVersion);
            AddNuSpecDepCore("Microsoft.CodeAnalysis.VisualBasic", CodeAnalysisVisualBasicVersion);
            AddNuSpecDep("System.Windows.Extensions", "4.6.0", tfmCore30);

            var files = new[] {
               new NuSpecContent{Source = Path.Combine(nugetDir, "**", "*.*"), Target = ""},
            };

            var nuGetPackSettings = new NuGetPackSettings
            {
                Id = "FastReport.Compat",
                Version = version,
                Authors = new[] { "Fast Reports Inc." },
                Owners = new[] { "Fast Reports Inc." },
                Description = "Common compatible types for FastReport .Net, Core and Mono",
                Repository = new NuGetRepository { Type = "GIT", Url = "https://github.com/FastReports/FastReport.Compat" },
                ProjectUrl = new Uri("https://www.fast-report.com/en/product/fast-report-net"),
                Icon = "frlogo192.png",
                IconUrl = new Uri("https://raw.githubusercontent.com/FastReports/FastReport.Compat/master/frlogo-big.png"),
                ReleaseNotes = new[] { "See the latest changes on https://github.com/FastReports/FastReport.Compat" },
                License = new NuSpecLicense { Type = "file", Value = "LICENSE.md" },
                Copyright = "Fast Reports Inc.",
                Tags = new[] { "reporting", "reports", "pdf", "html", "mvc", "docx", "xlsx", "Core" },
                RequireLicenseAcceptance = true,
                Symbols = false,
                NoPackageAnalysis = true,
                Files = files,
                Dependencies = dependencies,
                BasePath = nugetDir,
                OutputDirectory = outdir
            };

            // Pack
            NuGetPack(Path.Combine(solutionDirectory, "Nuget", nuGetPackSettings.Id + ".nuspec"), nuGetPackSettings);

            // Local functions:

            // For Net Standard 2.0, Standard 2.1, Core 3.0 and Net 5.0
            void AddNuSpecDepCore(string id, string version)
            {
                AddNuSpecDep(id, version, tfmStandard20);
                AddNuSpecDep(id, version, tfmStandard21);
                AddNuSpecDep(id, version, tfmCore30);
                AddNuSpecDep(id, version, tfmNet5win7);
            }

            void AddNuSpecDep(string id, string version, string tfm)
            {
                dependencies.Add(new NuSpecDependency { Id = id, Version = version, TargetFramework = tfm });
            }

            void TargetBuildCore(string target)
            {
                DotNetCoreMSBuild(solutionFile, new DotNetCoreMSBuildSettings()
                  .SetConfiguration(config)
                  .WithTarget(target)
                  .WithProperty("SolutionDir", solutionDirectory)
                  .WithProperty("SolutionFileName", solutionFilename)
                  .WithProperty("Version", version)
                );
            }

        }
    }
}
