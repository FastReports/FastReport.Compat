using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using static CakeScript.Startup;
using static CakeScript.CakeAPI;
using Cake.Common.IO;
using Cake.Common.Tools.DotNetCore.MSBuild;
using Cake.Common.Tools.NuGet.Pack;

namespace CakeScript;

partial class Program
{
    [DependsOn(nameof(PrepareNuget))]
    public void PackCompat()
    {
        const string packageId = "FastReport.Compat";
        string solutionFile = Path.Combine(solutionDirectory, solutionFilename);
        string usedPackagesVersionPath = Path.Combine(solutionDirectory, "UsedPackages.version");
        string resourcesDir = Path.Combine(solutionDirectory, "Nuget");
        string packCopyDir = Path.Combine(resourcesDir, packageId);

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
        
        string emptyFilePath = Path.Combine(nugetDir, "lib", "netcoreapp3.0", "_._");
        Directory.GetParent(emptyFilePath).Create();
        File.Create(emptyFilePath).Close();

        if (!File.Exists(emptyFilePath))
            throw new Exception($"Empty file wasn't created. '{emptyFilePath}'");

        // Get used packages version
        string SystemDrawingCommonVersion = XmlPeek(usedPackagesVersionPath, "//SystemDrawingCommonVersion/text()");
        Information($"System.Drawing.Common version: {SystemDrawingCommonVersion}");
        string CodeAnalysisCSharpVersion = XmlPeek(usedPackagesVersionPath, "//CodeAnalysisCSharpVersion/text()");
        Information($"Microsoft.CodeAnalysis.CSharp version: {CodeAnalysisCSharpVersion}");
        string CodeAnalysisVisualBasicVersion = XmlPeek(usedPackagesVersionPath, "//CodeAnalysisVisualBasicVersion/text()");
        Information($"Microsoft.CodeAnalysis.VisualBasic version: {CodeAnalysisVisualBasicVersion}");


        var dependencies = new List<NuSpecDependency>();
        AddNuSpecDep(null, null, tfmNet40);
        // System.Drawing.Common reference doesn't included in net5.0-windows target
        AddNuSpecDep("System.Drawing.Common", SystemDrawingCommonVersion, tfmStandard20);
        AddNuSpecDep("System.Drawing.Common", SystemDrawingCommonVersion, tfmStandard21);
        AddNuSpecDep("System.Drawing.Common", SystemDrawingCommonVersion, tfmCore30);
        AddNuSpecDepCore("Microsoft.CodeAnalysis.CSharp", CodeAnalysisCSharpVersion);
        AddNuSpecDepCore("Microsoft.CodeAnalysis.VisualBasic", CodeAnalysisVisualBasicVersion);

        var files = new[] {
           new NuSpecContent{Source = Path.Combine(nugetDir, "**", "*.*"), Target = ""},
           new NuSpecContent{Source = Path.Combine(packCopyDir, "**", "*.*"), Target = ""},
        };

        var nuGetPackSettings = new NuGetPackSettings
        {
            Id = packageId,
            Version = version,
            Authors = new[] { "Fast Reports Inc." },
            Owners = new[] { "Fast Reports Inc." },
            Description = "Common compatible types for FastReport .Net, Core and Mono",
            Repository = new NuGetRepository { Type = "GIT", Url = "https://github.com/FastReports/FastReport.Compat" },
            ProjectUrl = new Uri("https://www.fast-report.com/en/product/fast-report-net"),
            Icon = FRLOGO192PNG,
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
        var template = Path.Combine(resourcesDir, "template.nuspec");
        NuGetPack(template, nuGetPackSettings);

        // Local functions:

        // For Net Standard 2.0, Standard 2.1, Core 3.0 and Net 5.0-windows
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
            DotNetMSBuild(solutionFile, new DotNetCoreMSBuildSettings()
              .SetConfiguration(config)
              .WithTarget(target)
              .WithProperty("SolutionDir", solutionDirectory)
              .WithProperty("SolutionFileName", solutionFilename)
              .WithProperty("Version", version)
            );
        }

    }
}
