@echo off
setlocal ENABLEDELAYEDEXPANSION
rem HELP FOR THIS FILE
rem CLONE the fr if need
rem clear the fr if need
rem next run scripts from the fr tools
rem that's all

ECHO TRY TO BUILD FR.Compat

pushd .\build\Cake
   dotnet run --target=PackCompat --solution-filename=FastReport.Compat.sln --config=Release --vers=2022.2.1-dev
rem   dotnet run --target=PackCompatSkia --solution-filename=FastReport.Compat.Skia.sln --config=Release --vers=2022.2.1-dev
popd