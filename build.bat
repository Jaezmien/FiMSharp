@echo off

:: Build
echo [Build.bat] Building FiMSharp...
dotnet build FiMSharp -c Release -o bin/
echo [Build.bat] Building FiMSharp.Javascript...
dotnet build FiMSharp.Javascript -c Release -o bin/

:: Build test program (optional)
set osType=%1
if not "%osType%"=="" (
    
    echo [Build.bat] Building test program...
    set binDir=bin/%osType%
    dotnet clean -c Release -o "%binDir%"
    
    set buildCommand=dotnet publish FiMSharp.Test --self-contained=true -p:PublishSingleFile=True -c Release -o "%binDir%"

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