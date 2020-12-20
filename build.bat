@echo off

set osType=%1
if "%osType%"=="" (set osType=win)

set binDir=bin/%osType%

:: Clean previous builds
dotnet clean -c Release -o "%binDir%"
dotnet clean -c Debug -o "%binDir%"

:: Build
dotnet build FiMSharp -c Release -o "%binDir%"
dotnet build FiMSharp.Javascript -c Release -o "%binDir%"

:: Build test program
set buildCommand=dotnet publish FiMSharp.Test --self-contained=true -p:PublishSingleFile=True -c Release -o "%binDir%/Playground"

if "%osType%" == "win32" (
    %buildCommand% --runtime win-x86
)
if "%osType%" == "win" (
    %buildCommand% --runtime win-x64
)
if "%osType%" == "linux" (
    %buildCommand% --runtime linux-x64
)
if "%osType%" == "linuxarm" (
    %buildCommand% --runtime linux-arm
)
if "%osType%" == "darwin" (
    %buildCommand% --runtime osx-x64
)