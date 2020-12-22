@echo off
setlocal enabledelayedexpansion

:: Build
echo [Build.bat] Building FiMSharp...
dotnet build FiMSharp -c Release -o bin/

echo.
echo.
echo [Build.bat] Building FiMSharp.Javascript...
dotnet build FiMSharp.Javascript -c Release -o bin/

:: Build test program (optional)
if not "%1"=="" (
    
    set FiMos=%1
    set FiMbinDir=bin/!FiMos!

    echo.
    echo.
    echo [Build.bat] Building test program to "!FiMbinDir!"...
    dotnet clean -c Release -o "!FiMbinDir!"
    
    set buildCommand=dotnet publish FiMSharp.Test --self-contained=true -p:PublishSingleFile=True -c Release -o "!FiMbinDir!"

    if "!FiMos!" == "win32" (
        !buildCommand! --runtime win-x86
    )
    if "!FiMos!" == "win" (
        !buildCommand! --runtime win-x64
    )
    if "!FiMos!" == "linux" (
        !buildCommand! --runtime linux-x64
    )
    if "!FiMos!" == "linuxarm" (
        !buildCommand! --runtime linux-arm
    )
    if "!FiMos!" == "darwin" (
        !buildCommand! --runtime osx-x64
    )

)