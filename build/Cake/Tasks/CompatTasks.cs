using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using static CakeScript.Startup;
using static CakeScript.CakeAPI;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet.MSBuild;
using Cake.Common.Tools.NuGet.Pack;
using Cake.Common.Tools.DotNet.Build;
using Cake.Common.Tools.DotNet;

namespace CakeScript;

partial class Program
{
    [DependsOn(nameof(PrepareNuget))]
    public void PackCompat()
    {
        const string packageId = "FastReport.Compat";
        string usedPackagesVersionPath = Path.Combine(solutionDirectory, "UsedPackages.version");
        string resourcesDir = Path.Combine(solutionDirectory, "Nuget");
        string packCopyDir = Path.Combine(resourcesDir, packageId);

        string srcDir = Path.Combine(solutionDirectory, "src");
        string compatAnyDir = Path.Combine(srcDir, packageId);
        string compatAnyProj = Path.Combine(compatAnyDir, packageId + ".csproj");
        string compatWinDir = Path.Combine(srcDir, packageId + "-Windows");
        string compatWinProj = Path.Combine(compatWinDir, packageId + "-Windows.csproj");

        string tmpDir = Path.Combine(solutionDirectory, "tmp");

        if (DirectoryExists(tmpDir))
        {
            DeleteDirectory(tmpDir, new DeleteDirectorySettings
            {
                Force = true,
                Recursive = true
            });
        }

        DotNetClean(compatAnyProj);
        DotNetClean(compatWinProj);

        var buildSettings = new DotNetBuildSettings
        {
            Configuration = config,
            NoRestore = false,
            MSBuildSettings = new DotNetMSBuildSettings
            {
                Version = version,
            }.WithProperty("SolutionDir", solutionDirectory)
            .WithProperty("SolutionFileName", solutionFilename)
            .WithProperty("BaseOutputPath", tmpDir),
        };

        DotNetBuild(compatAnyProj, buildSettings);
        DotNetBuild(compatWinProj, buildSettings);

        string emptyFilePath = Path.Combine(tmpDir, "lib", "netcoreapp3.0", "_._");
        Directory.GetParent(emptyFilePath).Create();
        File.Create(emptyFilePath).Close();

        if (!File.Exists(emptyFilePath))
            throw new Exception($"Empty file wasn't created. '{emptyFilePath}'");

        // Runtime library
        string runtimeLib = Path.Combine(tmpDir, "runtimes", "any", "lib", "netcoreapp3.0");
        Directory.CreateDirectory(runtimeLib);
        string buildAnyLib = Path.Combine(tmpDir, "build", "netcoreapp3.0", "lib", "Any", "*.dll");
        CopyFiles(buildAnyLib, runtimeLib);

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

        const string license = "LICENSE.md";
        var files = new[] {
           new NuSpecContent{Source = Path.Combine(tmpDir, "**", "*.*"), Target = ""},
           new NuSpecContent{Source = Path.Combine(packCopyDir, "**", "*.*"), Target = ""},
           new NuSpecContent{Source = Path.Combine(solutionDirectory, FRLOGO192PNG), Target = "" },
           new NuSpecContent{Source = Path.Combine(solutionDirectory, license), Target = "" },
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
            License = new NuSpecLicense { Type = "file", Value = license },
            Copyright = "Fast Reports Inc.",
            Tags = new[] { "reporting", "reports", "pdf", "html", "mvc", "docx", "xlsx", "Core" },
            RequireLicenseAcceptance = true,
            Symbols = false,
            NoPackageAnalysis = true,
            Files = files,
            Dependencies = dependencies,
            BasePath = tmpDir,
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

    }
}
