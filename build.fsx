// include Fake libs
#r "./packages/FAKE/tools/FakeLib.dll"

open Fake
open Fake.Testing
open System
open System.IO

// Directories
let buildDir  = "./build/"
let deployDir = "./deploy/"

// Filesets
let appReferences  =
    !! "/**/*.csproj"
    ++ "/**/*.fsproj"

// version info
let version = "0.1"  // or retrieve from CI server

// Targets
Target "Clean" (fun _ ->
    CleanDirs [buildDir; deployDir]
)

Target "Build" (fun _ ->
    // compile all projects below src/app/
    MSBuildDebug buildDir "Build" appReferences
    |> Log "AppBuild-Output: "
)

Target "Deploy" (fun _ ->
    !! (buildDir + "/**/*.*")
    -- "*.zip"
    |> Zip buildDir (deployDir + "ApplicationName." + version + ".zip")
)

Target "NUnit" (fun _ ->
    !! "build/Pressevent.Test.dll"
    |> NUnit3 (fun p ->
        { p with
            Labels = LabelsLevel.All
            TimeOut = TimeSpan.FromMinutes 20.})
)

// Build order
"Clean"
  ==> "Build"
  ==> "NUnit"
  ==> "Deploy"



// Start build
RunTargetOrDefault "Deploy"
