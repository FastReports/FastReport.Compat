@echo off
setlocal ENABLEDELAYEDEXPANSION
rem HELP FOR THIS FILE
rem CLONE the fr if need
rem clear the fr if need
rem next run scripts from the fr tools
rem that's all

ECHO TRY TO BUILD FR.Compat

pushd .\build
   Powershell -ExecutionPolicy ByPass -File ".\build.ps1" --target=Compat --solution-filename=FastReport.Compat.sln --config=Release  --vers=2021.2.0
popd