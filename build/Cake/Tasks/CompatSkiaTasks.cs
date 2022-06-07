using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using static CakeScript.Startup;
using static CakeScript.CakeAPI;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet.MSBuild;
using Cake.Common.Tools.DotNet.Pack;

namespace CakeScript;

partial class Program
{

    [DependsOn(nameof(PrepareNuget))]
    public void PackCompatSkia()
    {
        const string packageId = "FastReport.Compat.Skia";
        string projectFile = Path.Combine(solutionDirectory, "src", packageId, packageId + ".csproj");

        TargetBuildCore("Clean");

        TargetBuildCore("Restore");

        TargetBuildCore("Build");

        var packSettings = new DotNetPackSettings
        {
            NoBuild = true,
            NoRestore = true,
            Configuration = config,
            OutputDirectory = outdir,
            MSBuildSettings = new DotNetMSBuildSettings
            {
                Version = version,
            }
        };

        DotNetPack(projectFile, packSettings);


        // Local functions:

        void TargetBuildCore(string target)
        {
            DotNetMSBuild(projectFile, new DotNetMSBuildSettings()
              .SetConfiguration(config)
              .WithTarget(target)
              .WithProperty("SolutionDir", solutionDirectory)
              .WithProperty("SolutionFileName", solutionFilename)
              .WithProperty("Version", version)
            );
        }
    }

}
