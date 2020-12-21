@echo off

set osType=%1
:: if "%osType%"=="" (set osType=win)

set binDir=bin/%osType%

:: Clean previous builds
echo [Build.bat] Cleaning...
dotnet clean -c Release -o "%binDir%"
dotnet clean -c Debug -o "%binDir%"

:: Build
echo [Build.bat] Building FiMSharp...
dotnet build FiMSharp -c Release -o bin/
echo [Build.bat] Building FiMSharp.Javascript...
dotnet build FiMSharp.Javascript -c Release -o bin/

:: Build test program (optional)
if not "%osType%"=="" (
    
    echo [Build.bat] Building test program...
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

)