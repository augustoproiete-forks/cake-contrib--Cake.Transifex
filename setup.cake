#addin "nuget:?package=Cake.Coverlet&version=2.0.1"
#load "nuget:https://ci.appveyor.com/nuget/cake-recipe-pylg5x5ru9c2?package=Cake.Recipe&prerelease&version=0.3.0-alpha0492"

Environment.SetVariableNames();

BuildParameters.SetParameters(
    context: Context,
    buildSystem: BuildSystem,
    sourceDirectoryPath: "./src",
    title: "Cake.Transifex",
    repositoryOwner: "cake-contrib",
    repositoryName: "Cake.Transifex",
    appVeyorAccountName: "cakecontrib",
    shouldRunDotNetCorePack: true,
    shouldBuildNugetSourcePackage: false,
    shouldDeployGraphDocumentation: false,
    solutionFilePath: "./Cake.Transifex.sln",
    testFilePattern: "/**/*.Tests.csproj",
    shouldRunCodecov: true,
    shouldExecuteGitLink: false,
    shouldRunGitVersion: true
);

ToolSettings.SetToolSettings(
    context: Context,
    dupFinderExcludePattern: new string[] {
        BuildParameters.RootDirectoryPath + "/src/*.Tests/**/*.cs"
    },
     dupFinderExcludeFilesByStartingCommentSubstring: new string[] {
         "<auto-generated>"
     },
     testCoverageFilter: "+[Cake.Transifex*]* -[*.Tests]*",
     testCoverageExcludeByAttribute: "*.ExcludeFromCodeCoverage*",
     testCoverageExcludeByFile: "*Designer.cs;*.g.cs;*.g.i.cs"
);

if (BuildParameters.IsRunningOnAppVeyor &&
    BuildParameters.IsMainRepository && BuildParameters.IsMasterBranch && !BuildParameters.IsTagged) {
    BuildParameters.Tasks.AppVeyorTask.IsDependentOn("Create-Release-Notes");
}

((CakeTask)BuildParameters.Tasks.DotNetCoreTestTask.Task).Actions.Clear();
((CakeTask)BuildParameters.Tasks.DotNetCoreTestTask.Task).Criterias.Clear();
((CakeTask)BuildParameters.Tasks.DotNetCoreTestTask.Task).Dependencies.Clear();

BuildParameters.Tasks.DotNetCoreTestTask
    .IsDependentOn("Install-ReportGenerator")
    .Does(() => {
    var projects = GetFiles(BuildParameters.TestDirectoryPath + (BuildParameters.TestFilePattern ?? "/**/*Tests.csproj"));
    var testFileName = BuildParameters.Paths.Files.TestCoverageOutputFilePath.GetFilename();
    var testDirectory = BuildParameters.Paths.Files.TestCoverageOutputFilePath.GetDirectory();

    var settings = new CoverletSettings {
        CollectCoverage = true,
        CoverletOutputFormat = CoverletOutputFormat.opencover,
        CoverletOutputDirectory = testDirectory,
        CoverletOutputName = testFileName.ToString(),
        MergeWithFile = BuildParameters.Paths.Files.TestCoverageOutputFilePath
    };
    foreach (var line in ToolSettings.TestCoverageExcludeByFile.Split(';')) {
        foreach (var file in GetFiles("**/" + line)) {
            settings = settings.WithFileExclusion(file.FullPath);
        }
    }

    var testSettings = new DotNetCoreTestSettings {
        Configuration = BuildParameters.Configuration,
        NoBuild = true
    };

    foreach (var project in projects) {
        DotNetCoreTest(project.FullPath, testSettings, settings);
    }

    if (FileExists(BuildParameters.Paths.Files.TestCoverageOutputFilePath)) {
        ReportGenerator(BuildParameters.Paths.Files.TestCoverageOutputFilePath, BuildParameters.Paths.Directories.TestCoverage);
    }
});

BuildParameters.Tasks.TransifexPushSourceResource.WithCriteria(() => BuildParameters.IsRunningOnWindows);

Task("Appveyor-Linux")
    .IsDependentOn("Upload-AppVeyor-Artifacts");

BuildParameters.PrintParameters(Context);

Build.RunDotNetCore();
